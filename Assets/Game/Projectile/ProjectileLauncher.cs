using UnityEngine;
using UnityEngine.InputSystem;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Projectile
{
    public class ProjectileLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float speed = 32f;
        [SerializeField] private float projectileMass = 1f;
        [SerializeField] private float gravityMultiplier = 1f;

        [Header("References")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Damage")]
        [SerializeField] private float damage = 50f;

        [Header("Cooldown")]
        [SerializeField] private float fireCooldown = 0.6f;

        private float _lastFireTime = -999f;
        private CannonRecoil _recoil;
        private CameraController _cameraCtrl;
        private ProjectileCamera _projCam;

        private void Start()
        {
            _recoil     = GetComponentInParent<CannonRecoil>();
            _cameraCtrl = Camera.main?.GetComponent<CameraController>();
            _projCam    = Camera.main?.GetComponent<ProjectileCamera>();
        }

        private void Update()
        {
            if (CanFire() && FireKeyDown())
                Fire();
        }

        // UI 按鈕（Android）呼叫
        public void OnFireButtonPressed()
        {
            if (CanFire()) Fire();
        }

        private bool CanFire() => Time.time - _lastFireTime >= fireCooldown;

        private bool FireKeyDown()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return TouchFirePressed();
#endif
        }

        private bool TouchFirePressed()
        {
            if (Touchscreen.current == null) return false;
            foreach (var touch in Touchscreen.current.touches)
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began) return true;
            return false;
        }

        private void Fire()
        {
            _lastFireTime = Time.time;

            Transform spawnPoint = muzzle != null ? muzzle : transform;

            var proj = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

            if (!proj.TryGetComponent<Rigidbody>(out var rb))
                rb = proj.AddComponent<Rigidbody>();

            rb.mass       = projectileMass;
            rb.useGravity = true;

            if (!Mathf.Approximately(gravityMultiplier, 1f))
            {
                var cf = proj.AddComponent<ConstantForce>();
                cf.force = Physics.gravity * rb.mass * (gravityMultiplier - 1f);
            }

            rb.linearVelocity = spawnPoint.forward * speed;

            // 確保砲彈有 VFX 組件，並傳入傷害值
            if (!proj.TryGetComponent<ProjectileVFX>(out var vfx))
                vfx = proj.AddComponent<ProjectileVFX>();
            vfx.Damage = damage;

            Destroy(proj, 12f);

            // 後座力
            _recoil?.Trigger();

            // 切換側面追蹤視角（看砲彈弧線與落點）
            _projCam?.StartTracking(proj.transform, spawnPoint);

            // 攝影機衝擊（追蹤中由 CameraController 停用，回歸後才生效）
            _cameraCtrl?.AddTrauma(0.45f);
        }

        public float GetSpeed()              => speed;
        public float GetGravityMultiplier()  => gravityMultiplier;
        public Transform GetMuzzle()         => muzzle != null ? muzzle : transform;
    }
}
