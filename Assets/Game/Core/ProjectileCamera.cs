using UnityEngine;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 發射後自動切換至側面追蹤視角，看完落點後平滑返回瞄準視角。
    /// 掛在 Main Camera 上，不依賴 Cinemachine。
    /// </summary>
    public class ProjectileCamera : MonoBehaviour
    {
        [Header("側面視角")]
        [SerializeField] private float sideOffset   = 18f;   // 與彈道垂直的橫向距離
        [SerializeField] private float heightOffset = 6f;    // 相機高度（相對砲口）
        [SerializeField] private float trackSmooth  = 5f;    // 跟蹤平滑係數

        [Header("時序")]
        [SerializeField] private float toSideTime   = 0.4f;  // 切換到側視花費時間
        [SerializeField] private float returnDelay  = 1.8f;  // 落點後停留時間
        [SerializeField] private float returnTime   = 0.65f; // 返回瞄準視角花費時間

        // ── 狀態機 ───────────────────────────────────────────────────
        private enum CamState { Aiming, ToSide, Tracking, Waiting, ToAim }
        private CamState _state = CamState.Aiming;

        // ── 參考 ─────────────────────────────────────────────────────
        private CameraController _camCtrl;
        private Transform _aimParent;      // BarrelPivot
        private Vector3   _aimLocalPos;
        private Quaternion _aimLocalRot;

        // ── 追蹤資料 ─────────────────────────────────────────────────
        private Transform _target;         // 砲彈 Transform
        private Vector3   _sideDir;        // 彈道的水平垂直方向
        private float     _blendT;
        private float     _waitTimer;
        private Vector3   _fromPos;
        private Quaternion _fromRot;

        // ── 初始化 ───────────────────────────────────────────────────
        private void Awake()
        {
            _camCtrl    = GetComponent<CameraController>();
            _aimParent  = transform.parent;
            _aimLocalPos = transform.localPosition;
            _aimLocalRot = transform.localRotation;
        }

        // ── 每幀更新 ─────────────────────────────────────────────────
        private void LateUpdate()
        {
            switch (_state)
            {
                case CamState.Aiming:   break;
                case CamState.ToSide:   StepToSide();   break;
                case CamState.Tracking: StepTracking(); break;
                case CamState.Waiting:
                    _waitTimer -= Time.deltaTime;
                    if (_waitTimer <= 0f) BeginReturn();
                    break;
                case CamState.ToAim: StepToAim(); break;
            }
        }

        // ── 公開 API：Launcher 呼叫 ──────────────────────────────────
        /// <summary>發射後呼叫，傳入砲彈 Transform 和砲口 Transform。</summary>
        public void StartTracking(Transform projectile, Transform muzzle)
        {
            // 正在追蹤中，跳過（不中斷現有鏡頭）
            if (_state != CamState.Aiming) return;

            _target = projectile;

            // 側方向 = 水平彈道的右法線
            Vector3 flatForward = new Vector3(muzzle.forward.x, 0f, muzzle.forward.z);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            flatForward.Normalize();
            _sideDir = Vector3.Cross(Vector3.up, flatForward); // 砲口右側

            // 記錄目前姿態供插值用
            _fromPos = transform.position;
            _fromRot = transform.rotation;
            _blendT  = 0f;

            // 脫離 BarrelPivot，進入世界空間自由移動
            transform.SetParent(null);
            if (_camCtrl != null) _camCtrl.enabled = false;

            _state = CamState.ToSide;
        }

        // ── 狀態：過渡到側面 ─────────────────────────────────────────
        private void StepToSide()
        {
            _blendT += Time.deltaTime / toSideTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendT));

            Vector3 idealPos = CalcSidePos();
            transform.position = Vector3.Lerp(_fromPos, idealPos, t);

            // 看向砲彈（或砲口前方）
            Vector3 lookAt = _target != null ? _target.position : idealPos - _sideDir * sideOffset;
            PointAt(lookAt, t, _fromRot);

            if (_blendT >= 1f)
                _state = _target != null ? CamState.Tracking : CamState.Waiting;
        }

        // ── 狀態：跟蹤砲彈 ──────────────────────────────────────────
        private void StepTracking()
        {
            if (_target == null)
            {
                // 砲彈已銷毀（落地）
                _waitTimer = returnDelay;
                _state = CamState.Waiting;
                return;
            }

            // 相機沿側方向平行滑動，始終保持橫向固定距離
            Vector3 ideal = CalcSidePos();
            transform.position = Vector3.Lerp(transform.position, ideal, Time.deltaTime * trackSmooth);

            // 看向砲彈
            Vector3 dir = _target.position - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // ── 狀態：返回瞄準 ───────────────────────────────────────────
        private void BeginReturn()
        {
            _fromPos = transform.position;
            _fromRot = transform.rotation;
            _blendT  = 0f;
            _state   = CamState.ToAim;
        }

        private void StepToAim()
        {
            _blendT += Time.deltaTime / returnTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendT));

            // 目標 = BarrelPivot 目前的世界空間位置（跟著炮台轉）
            Vector3 aimWorld    = _aimParent != null ? _aimParent.TransformPoint(_aimLocalPos) : _fromPos;
            Quaternion aimRot   = _aimParent != null ? _aimParent.rotation * _aimLocalRot      : _fromRot;

            transform.position = Vector3.Lerp(_fromPos, aimWorld, t);
            transform.rotation = Quaternion.Slerp(_fromRot, aimRot, t);

            if (_blendT >= 1f)
            {
                // 重新掛回 BarrelPivot
                transform.SetParent(_aimParent);
                transform.localPosition = _aimLocalPos;
                transform.localRotation = _aimLocalRot;

                if (_camCtrl != null) _camCtrl.enabled = true;
                _target = null;
                _state  = CamState.Aiming;
            }
        }

        // ── 計算理想側面位置 ─────────────────────────────────────────
        private Vector3 CalcSidePos()
        {
            Vector3 anchor = _target != null ? _target.position : transform.position;
            // 水平側移 + 高度偏移（相對砲彈位置，讓相機滑動跟上弧線）
            return new Vector3(
                anchor.x + _sideDir.x * sideOffset,
                anchor.y + heightOffset,
                anchor.z + _sideDir.z * sideOffset);
        }

        // ── 帶 t 插值的 LookAt ───────────────────────────────────────
        private void PointAt(Vector3 target, float t, Quaternion fromRot)
        {
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude < 0.01f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(fromRot, targetRot, t);
        }
    }
}
