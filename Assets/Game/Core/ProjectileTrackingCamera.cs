using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtilleryFrontier.Core
{
    public class ProjectileTrackingCamera : MonoBehaviour
    {
        [SerializeField] private float sideDistance  = 22f;
        [SerializeField] private float heightOffset  = 8f;
        [SerializeField] private float followSmooth  = 6f;
        [SerializeField] private float rotateSmooth  = 8f;
        [SerializeField] private float toSideTime    = 0.5f;
        [SerializeField] private float hitDwellTime  = 1f;
        [SerializeField] private float returnTime    = 0.6f;

        [Header("Auto Track")]
        // 波次防禦需要連發，預設關閉自動追彈（仍可按住 C 手動看彈）。
        [SerializeField] private bool  autoTrackProjectile = false;
        [SerializeField] private float autoTrackDelay      = 0.3f;

        private enum State { Idle, ToSide, Following, HitDwell, Returning }
        private State _state = State.Idle;

        private Transform  _projectile;
        private Vector3    _sideDir;
        private float      _dwellTimer;
        private float      _blendT;
        private Vector3    _fromPos;
        private Quaternion _fromRot;
        private float      _autoTrackTimer = -1f;
        private bool       _isAutoTracking;

        private CameraController _ctrl;
        private Transform        _aimParent;
        private Vector3          _aimLocalPos;
        private Quaternion       _aimLocalRot;

        private void Awake()
        {
            _ctrl        = GetComponent<CameraController>();
            _aimParent   = transform.parent;
            _aimLocalPos = transform.localPosition;
            _aimLocalRot = transform.localRotation;
        }

        public void SetPendingTarget(Transform projectile, Transform muzzle)
        {
            _projectile = projectile;

            Vector3 flat = new Vector3(muzzle.forward.x, 0f, muzzle.forward.z);
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
            _sideDir = Vector3.Cross(Vector3.up, flat.normalized);

            // 發射後啟動自動追彈計時器（僅閒置時）
            if (autoTrackProjectile && _state == State.Idle)
                _autoTrackTimer = autoTrackDelay;
        }

        private void LateUpdate()
        {
            if (CameraDirector.IsObserving)
            {
                _autoTrackTimer = -1f;  // 觀測中取消計時
                return;
            }

            bool c = CKeyHeld();

            switch (_state)
            {
                case State.Idle:
                    // 自動追彈計時
                    if (autoTrackProjectile && _autoTrackTimer >= 0f)
                    {
                        _autoTrackTimer -= Time.deltaTime;
                        if (_autoTrackTimer <= 0f)
                        {
                            _autoTrackTimer = -1f;
                            if (_projectile != null)
                            {
                                _isAutoTracking = true;
                                BeginTracking();
                                break;
                            }
                        }
                    }
                    // 手動 C 鍵
                    if (c && _projectile != null)
                    {
                        _isAutoTracking = false;
                        BeginTracking();
                    }
                    break;

                case State.ToSide:
                    // 自動追彈中不需 C 鍵；手動模式放開 C 則取消
                    if (!_isAutoTracking && !c) { BeginReturn(); break; }
                    StepToSide();
                    break;

                case State.Following:
                    if (_projectile == null) { BeginDwell(); break; }
                    if (!_isAutoTracking && !c) { BeginReturn(); break; }
                    StepFollow();
                    break;

                case State.HitDwell:
                    _dwellTimer -= Time.deltaTime;
                    // 時間到 或 任意按鍵 → 返回
                    if (_dwellTimer <= 0f || AnyInput()) BeginReturn();
                    break;

                case State.Returning:
                    StepReturn();
                    break;
            }
        }

        private void BeginTracking()
        {
            // 僅能從 Aim 進入 Track（與 Observe 互斥）
            if (CameraDirector.Instance == null ||
                !CameraDirector.Instance.TryEnter(CameraDirector.Mode.Track))
            {
                _isAutoTracking = false;
                return;
            }

            _fromPos = transform.position;
            _fromRot = transform.rotation;
            _blendT  = 0f;
            transform.SetParent(null);
            if (_ctrl != null) _ctrl.enabled = false;
            _state = State.ToSide;
        }

        private void StepToSide()
        {
            _blendT += Time.deltaTime / toSideTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendT));

            Vector3 ideal = CalcSidePos();
            transform.position = Vector3.Lerp(_fromPos, ideal, t);

            Vector3 lookTarget = _projectile != null ? _projectile.position : ideal + Vector3.forward;
            transform.rotation = Quaternion.Slerp(_fromRot, SafeLook(lookTarget - transform.position), t);

            if (_blendT >= 1f)
                _state = _projectile != null ? State.Following : State.HitDwell;
        }

        private void StepFollow()
        {
            Vector3 ideal = CalcSidePos();
            transform.position = Vector3.Lerp(transform.position, ideal, Time.deltaTime * followSmooth);

            Vector3 dir = _projectile.position - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    SafeLook(dir), Time.deltaTime * rotateSmooth);
        }

        private void BeginDwell()
        {
            _dwellTimer = hitDwellTime;
            _state = State.HitDwell;
        }

        private void BeginReturn()
        {
            _fromPos        = transform.position;
            _fromRot        = transform.rotation;
            _blendT         = 0f;
            _projectile     = null;
            _isAutoTracking = false;
            _state          = State.Returning;
        }

        private void StepReturn()
        {
            _blendT += Time.deltaTime / returnTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendT));

            Vector3    aimWorld = _aimParent != null ? _aimParent.TransformPoint(_aimLocalPos) : _fromPos;
            Quaternion aimRot   = _aimParent != null ? _aimParent.rotation * _aimLocalRot      : _fromRot;

            transform.position = Vector3.Lerp(_fromPos, aimWorld, t);
            transform.rotation = Quaternion.Slerp(_fromRot, aimRot, t);

            if (_blendT >= 1f)
            {
                transform.SetParent(_aimParent);
                transform.localPosition = _aimLocalPos;
                transform.localRotation = _aimLocalRot;
                if (_ctrl != null) _ctrl.enabled = true;
                _state = State.Idle;
                CameraDirector.Instance?.Exit(CameraDirector.Mode.Track);
            }
        }

        private Vector3 CalcSidePos()
        {
            Vector3 anchor = _projectile != null ? _projectile.position : transform.position;
            return new Vector3(
                anchor.x + _sideDir.x * sideDistance,
                anchor.y + heightOffset,
                anchor.z + _sideDir.z * sideDistance);
        }

        private static Quaternion SafeLook(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return Quaternion.identity;
            return Quaternion.LookRotation(dir);
        }

        private static bool CKeyHeld()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var kb = Keyboard.current;
            return kb != null && kb.cKey.isPressed;
#else
            return false;
#endif
        }

        // 任意輸入（命中停留期間提前返回）
        private static bool AnyInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var ms = Mouse.current;
            if (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame))
                return true;
            var kb = Keyboard.current;
            return kb != null && kb.anyKey.wasPressedThisFrame;
#else
            var ts = Touchscreen.current;
            if (ts == null) return false;
            foreach (var t in ts.touches)
                if (t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began) return true;
            return false;
#endif
        }

        private void OnDestroy() { CameraDirector.Instance?.Exit(CameraDirector.Mode.Track); }
    }
}
