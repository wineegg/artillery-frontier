using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtilleryFrontier.Combat
{
    public class ArtilleryController : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float angleSpeed = 45f;

        [Header("Elevation Limits")]
        [SerializeField] private float minAngle = 0f;
        [SerializeField] private float maxAngle = 80f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.05f;

        [Header("References")]
        [SerializeField] private Transform barrelPivot;

        private float _currentYaw;
        private float _currentPitch;
        private float _targetYaw;
        private float _targetPitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector2 _touchDelta;
        private bool _isDragging;
        private int _activeTouchId = -1;

        private void Awake()
        {
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = barrelPivot != null ? barrelPivot.localEulerAngles.x : 0f;
            _targetYaw   = _currentYaw;
            _targetPitch = _currentPitch;
        }

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
            if (Mouse.current == null) return;

            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _touchDelta = delta * 0.1f;
            }
            else
            {
                _touchDelta = Vector2.zero;
            }
        }

        private void HandleTouchInput()
        {
            if (Touchscreen.current == null) return;

            var touches = Touchscreen.current.touches;

            foreach (var touch in touches)
            {
                var phase = touch.phase.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Began && _activeTouchId == -1)
                {
                    _activeTouchId = touch.touchId.ReadValue();
                    _isDragging = true;
                }

                if (touch.touchId.ReadValue() == _activeTouchId)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        _touchDelta = touch.delta.ReadValue() * 0.05f;
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                             phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        _touchDelta = Vector2.zero;
                        _activeTouchId = -1;
                        _isDragging = false;
                    }
                    else
                    {
                        _touchDelta = Vector2.zero;
                    }
                }
            }
        }

        private void ApplyRotation()
        {
            // 累積到 target，actual 用 SmoothDamp 跟上（慣性感）
            _targetYaw += _touchDelta.x * rotationSpeed * Time.deltaTime;
            _targetYaw = Mathf.Clamp(_targetYaw, -90f, 90f);

            // += : 滑鼠上移 / 手指上滑 → pitch 增加 → 仰角提高
            _targetPitch += _touchDelta.y * angleSpeed * Time.deltaTime;
            _targetPitch = Mathf.Clamp(_targetPitch, minAngle, maxAngle);

            _currentYaw   = Mathf.SmoothDamp(_currentYaw,   _targetYaw,   ref _yawVelocity,   smoothTime);
            _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, smoothTime);

            transform.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);

            // 負號：Unity Euler X 正值朝下，負值才是仰角升高
            if (barrelPivot != null)
                barrelPivot.localRotation = Quaternion.Euler(-_currentPitch, 0f, 0f);
        }

        public float GetCurrentYaw()   => _currentYaw;
        public float GetCurrentPitch() => _currentPitch;
        public float GetTargetYaw()    => _targetYaw;
        public float GetTargetPitch()  => _targetPitch;

        // HUD 刻度觸控直接設定目標角度
        public void SetTargetYaw(float yaw)     => _targetYaw   = Mathf.Clamp(yaw, -90f, 90f);
        public void SetTargetPitch(float pitch) => _targetPitch = Mathf.Clamp(pitch, minAngle, maxAngle);

        public Transform GetBarrelPivot() => barrelPivot;
    }
}
