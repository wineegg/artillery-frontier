using UnityEngine;
using UnityEngine.InputSystem;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Projectile
{
    public class ProjectileLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform  muzzle;
        [SerializeField] private GameObject projectilePrefab;

        private float _lastFireTime = -999f;

        private CannonRecoil            _recoil;
        private CameraController        _cameraCtrl;
        private ProjectileTrackingCamera _tracking;

        private void Start()
        {
            _recoil     = GetComponentInParent<CannonRecoil>();
            _cameraCtrl = Camera.main?.GetComponent<CameraController>();
            _tracking   = Camera.main?.GetComponent<ProjectileTrackingCamera>();
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

        private bool CanFire() => Time.time - _lastFireTime >= GameConfig.FireCooldown;

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

            Transform spawn = muzzle != null ? muzzle : transform;

            var go = Instantiate(projectilePrefab, spawn.position, spawn.rotation);

            // 確定性運動學砲彈：以 GameConfig 初速沿砲口方向發射
            if (!go.TryGetComponent<Projectile>(out var proj))
                proj = go.AddComponent<Projectile>();
            proj.Ammo = AmmoSelector.Current;
            proj.Launch(spawn.forward * GameConfig.MuzzleSpeed);

            _recoil?.Trigger();
            _tracking?.SetPendingTarget(go.transform, spawn);
            _cameraCtrl?.AddTrauma(GameConfig.FireTrauma);
        }

        public Transform GetMuzzle() => muzzle != null ? muzzle : transform;
    }
}
