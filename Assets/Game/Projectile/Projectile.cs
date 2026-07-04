using UnityEngine;
using ArtilleryFrontier.Core;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Projectile
{
    /// <summary>
    /// 確定性運動學砲彈。FixedUpdate 以 Ballistics.Step 積分（與 LandingPreview 同一套），
    /// 因此落點 = 預測落點。命中偵測用線段 SphereCast（防穿透）+ 解析地面查詢。
    /// 取代舊的 Rigidbody + OnCollisionEnter 流程。
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class Projectile : MonoBehaviour
    {
        public AmmoType Ammo { get; set; } = AmmoType.Normal;

        private Vector3 _vel;
        private bool    _live;
        private float   _age;

        private void Awake()
        {
            // 運動學砲彈自行處理碰撞：移除自身 Collider / Rigidbody 以免變成靜態碰撞體。
            if (TryGetComponent<Collider>(out var col))  Destroy(col);
            if (TryGetComponent<Rigidbody>(out var rb))  Destroy(rb);
        }

        public void Launch(Vector3 velocity)
        {
            _vel  = velocity;
            _live = true;
            if (velocity.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(velocity);
        }

        private void FixedUpdate()
        {
            if (!_live) return;

            float   dt   = Time.fixedDeltaTime;
            Vector3 prev = transform.position;

            Vector3 pos = prev, vel = _vel;
            Ballistics.Step(ref pos, ref vel, dt);
            _vel  = vel;
            _age += dt;

            // ── 目標命中：線段 SphereCast（排除地形，地面走解析查詢）──
            Vector3 seg  = pos - prev;
            float   dist = seg.magnitude;
            if (dist > 1e-4f &&
                Physics.SphereCast(prev, GameConfig.ProjectileRadius, seg / dist,
                                   out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore) &&
                !(hit.collider is TerrainCollider))
            {
                var target = hit.collider.GetComponentInParent<DestructibleTarget>();
                Impact(hit.point, hit.normal, target);
                return;
            }

            // ── 地面命中：解析高度（與預測完全一致）──
            float gy = Ballistics.GroundY(pos);
            if (pos.y <= gy)
            {
                pos.y = gy;
                Impact(pos, Vector3.up, null);
                return;
            }

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(_vel);

            if (_age >= GameConfig.ProjectileLifetime)
                Destroy(gameObject);
        }

        private void Impact(Vector3 point, Vector3 normal, DestructibleTarget target)
        {
            _live = false;

            var spec = AmmoConfig.Get(Ammo);

            if (spec.aoeRadius > 0f)
            {
                // 範圍爆炸：命中點半徑內所有目標
                foreach (var col in Physics.OverlapSphere(point, spec.aoeRadius, ~0,
                             QueryTriggerInteraction.Ignore))
                {
                    if (col is TerrainCollider) continue;
                    col.GetComponentInParent<DestructibleTarget>()?.Impact(spec.damage, spec.ignoreArmor);
                }
            }
            else if (target != null)
            {
                target.Impact(spec.damage, spec.ignoreArmor);
                if (target is Enemy e)
                {
                    if (spec.burnDps > 0f) e.ApplyBurn(spec.burnDps, spec.burnTime);
                    if (spec.slowFactor > 0f && spec.slowFactor < 1f) e.ApplySlow(spec.slowFactor, spec.slowTime);
                }
            }

            var cam    = Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, point) : 0f;

            ImpactEffect.Spawn(point, normal);
            ImpactMarker.Spawn(point, dist, target);

            if (cam != null && cam.TryGetComponent<CameraController>(out var cc))
                cc.AddTrauma(GameConfig.ImpactTrauma);

            Destroy(gameObject);
        }
    }
}
