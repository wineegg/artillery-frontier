using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Core;
using ArtilleryFrontier.Projectile;

namespace ArtilleryFrontier.Combat
{
    public class ArtilleryController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float angleSpeed    = 45f;

        [Header("Elevation Limits")]
        [SerializeField] private float minAngle = 0f;
        [SerializeField] private float maxAngle = 80f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.05f;

        [Header("Fine Control")]
        [SerializeField] private float scrollPitchStep = 0.012f;   // °/scroll-unit

        [Header("References")]
        [SerializeField] private Transform barrelPivot;

        private float   _currentYaw;
        private float   _currentPitch;
        private float   _targetYaw;
        private float   _targetPitch;
        private float   _yawVelocity;
        private float   _pitchVelocity;
        private Vector2 _touchDelta;
        private bool    _isDragging;
        private int     _activeTouchId = -1;

        private ProjectileLauncher _launcher;

        /// 本幀玩家是否有手動拖曳瞄準（用於解除目標鎖定）。
        public bool ManualAimActive { get; private set; }

        /// 最近一次 AimAtTarget 是否有有效解（false = 射程不足）。
        public bool LastSolutionValid { get; private set; } = true;

        /// 當前砲口是否已對準目標角（可發射）。
        public bool AimConverged =>
            Mathf.Abs(Mathf.DeltaAngle(_currentYaw, _targetYaw)) < 2f &&
            Mathf.Abs(_currentPitch - _targetPitch) < 1.5f;

        // ── 初始化 ───────────────────────────────────────────────────
        private void Awake()
        {
            _currentYaw   = transform.eulerAngles.y;
            _currentPitch = barrelPivot != null ? barrelPivot.localEulerAngles.x : 0f;
            _targetYaw    = _currentYaw;
            _targetPitch  = _currentPitch;
        }

        private void Start()
        {
            _launcher = GetComponentInChildren<ProjectileLauncher>();
        }

        /// 自動瞄準：以彈道解算設定 Yaw + 仰角。移動目標會用飛行時間做單次提前量預測。
        public void AimAtTarget(Vector3 targetPos, Vector3 targetVel = default)
        {
            Vector3 muzzle = _launcher != null ? _launcher.GetMuzzle().position
                           : (barrelPivot != null ? barrelPivot.position : transform.position);

            // 提前量：二次迭代收斂（用飛行時間把目標往前推），移動目標更準
            float speed = GameConfig.MuzzleSpeed;
            Vector3 aimPoint = targetPos;
            if (targetVel.sqrMagnitude > 0.01f)
            {
                for (int i = 0; i < 2; i++)
                {
                    float p = Ballistics.SolveElevation(muzzle, aimPoint, speed);
                    if (float.IsNaN(p)) break;
                    float x   = new Vector2(aimPoint.x - muzzle.x, aimPoint.z - muzzle.z).magnitude;
                    float tof = x / Mathf.Max(speed * Mathf.Cos(p * Mathf.Deg2Rad), 0.01f);
                    aimPoint  = targetPos + targetVel * tof;
                }
            }

            float yaw   = Ballistics.SolveYaw(muzzle, aimPoint);
            float pitch = Ballistics.SolveElevation(muzzle, aimPoint, speed);

            LastSolutionValid = !float.IsNaN(pitch);
            _targetYaw = Mathf.Clamp(yaw, -90f, 90f);
            if (LastSolutionValid)
                _targetPitch = Mathf.Clamp(pitch, minAngle, maxAngle);
        }

        // ── 主循環 ───────────────────────────────────────────────────
        private void Update()
        {
            HandleInput();
            ApplyRotation();
        }

        private void HandleInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            if (Mouse.current == null || !CameraDirector.IsAiming) return;

            // Shift 精細模式（精度 × 0.15）
            bool  shift   = Keyboard.current?.shiftKey.isPressed ?? false;
            float fineMul = shift ? 0.15f : 1f;

            // 左鍵拖拽：粗調 Yaw + Pitch（指標在 UI 上時不拖炮，避免點標記/FIRE 誤觸）
            ManualAimActive = false;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUI && Mouse.current.leftButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _touchDelta = delta * 0.1f * fineMul;
                if (delta.sqrMagnitude > 0.5f) ManualAimActive = true;   // 有實際拖曳 → 解除鎖定
            }
            else
            {
                _touchDelta = Vector2.zero;
            }

            // 滾輪：Pitch 微調
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _targetPitch = Mathf.Clamp(
                    _targetPitch + scroll * scrollPitchStep, minAngle, maxAngle);
        }

        private void HandleTouchInput()
        {
            ManualAimActive = false;
            if (Touchscreen.current == null) return;

            var touches = Touchscreen.current.touches;
            foreach (var touch in touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began && _activeTouchId == -1)
                {
                    _activeTouchId = touch.touchId.ReadValue();
                    _isDragging    = true;
                }
                if (touch.touchId.ReadValue() == _activeTouchId)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        _touchDelta = touch.delta.ReadValue() * 0.05f;
                        if (_touchDelta.sqrMagnitude > 0.01f) ManualAimActive = true;
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                             phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        _touchDelta    = Vector2.zero;
                        _activeTouchId = -1;
                        _isDragging    = false;
                    }
                    else
                        _touchDelta = Vector2.zero;
                }
            }
        }

        private void ApplyRotation()
        {
            _targetYaw  += _touchDelta.x * rotationSpeed * Time.deltaTime;
            _targetYaw   = Mathf.Clamp(_targetYaw, -90f, 90f);
            _targetPitch += _touchDelta.y * angleSpeed * Time.deltaTime;
            _targetPitch  = Mathf.Clamp(_targetPitch, minAngle, maxAngle);

            _currentYaw   = Mathf.SmoothDamp(_currentYaw,   _targetYaw,   ref _yawVelocity,   smoothTime);
            _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, smoothTime);

            transform.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
            if (barrelPivot != null)
                barrelPivot.localRotation = Quaternion.Euler(-_currentPitch, 0f, 0f);
        }

        // ── 公開 API ─────────────────────────────────────────────────
        public float GetCurrentYaw()   => _currentYaw;
        public float GetCurrentPitch() => _currentPitch;
        public float GetTargetYaw()    => _targetYaw;
        public float GetTargetPitch()  => _targetPitch;

        public void SetTargetYaw(float yaw)     => _targetYaw   = Mathf.Clamp(yaw, -90f, 90f);
        public void SetTargetPitch(float pitch) => _targetPitch = Mathf.Clamp(pitch, minAngle, maxAngle);

        public Transform GetBarrelPivot() => barrelPivot;
    }
}
