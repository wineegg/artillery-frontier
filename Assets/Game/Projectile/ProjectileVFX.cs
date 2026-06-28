using UnityEngine;
using ArtilleryFrontier.Core;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Projectile
{
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileVFX : MonoBehaviour
    {
        private void Start()
        {
            // 砲彈本體用亮橘色，確保在 URP 下可見（Default 材質在 URP 是粉紅）
            var r = GetComponent<Renderer>();
            if (r != null)
                r.material = SpriteMat(new Color(1f, 0.55f, 0.05f, 1f));

            SpawnMuzzleFlash();
            AttachSmokeTrail();
        }

        private void OnCollisionEnter(Collision col)
        {
            ImpactEffect.Spawn(col.contacts[0].point, col.contacts[0].normal);

            var cam = Camera.main;
            if (cam != null && cam.TryGetComponent<CameraController>(out var cc))
                cc.AddTrauma(0.55f);

            Destroy(gameObject);
        }

        // ── 砲口火光 ─────────────────────────────────────────────────
        private void SpawnMuzzleFlash()
        {
            var root = new GameObject("MuzzleFlash");
            root.transform.position = transform.position;
            root.transform.rotation = transform.rotation;

            FlashBurst(root, size: 0.5f,  count: 6,  color: new Color(1f, 0.85f, 0.3f), speed: 3f,   lifetime: 0.10f, additive: true);
            SparkBurst(root, count: 8,   color: new Color(1f, 0.55f, 0.1f));
            MuzzleSmoke(root);

            Destroy(root, 0.4f);
        }

        private static void FlashBurst(GameObject parent, float size, int count,
                                        Color color, float speed, float lifetime, bool additive)
        {
            var ps = MakePS("Flash", parent);
            var m  = ps.main;
            m.duration    = 0.05f;
            m.loop        = false;
            m.startLifetime = lifetime;
            m.startSpeed  = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            m.startSize   = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            m.startColor  = color;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = count * 2;

            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle     = 25f;
            sh.radius    = 0.05f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            ApplyMat(ps, color, additive);
            ps.Play();
        }

        private static void SparkBurst(GameObject parent, int count, Color color)
        {
            var ps = MakePS("Sparks", parent);
            var m  = ps.main;
            m.duration    = 0.05f;
            m.loop        = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            m.startSpeed  = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize   = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            m.startColor  = color;
            m.gravityModifier = 0.8f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 40f;
            sh.radius = 0.05f;

            ApplyMat(ps, color, additive: true);
            ps.Play();
        }

        private static void MuzzleSmoke(GameObject parent)
        {
            var ps = MakePS("Smoke", parent);
            var m  = ps.main;
            m.duration    = 0.2f;
            m.loop        = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            m.startSpeed  = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            m.startSize   = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            m.startColor  = new ParticleSystem.MinMaxGradient(
                new Color(0.5f, 0.5f, 0.5f, 0.6f),
                new Color(0.7f, 0.65f, 0.55f, 0.3f));
            m.gravityModifier = -0.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 15;

            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.2f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));

            ApplyMat(ps, new Color(0.6f, 0.6f, 0.6f), additive: false);
            ps.Play();
        }

        // ── 飛行尾煙（側視時易辨認）────────────────────────────────
        private void AttachSmokeTrail()
        {
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time               = 0.9f;
            trail.startWidth         = 0.28f;   // 夠寬，18m 側視清晰可見
            trail.endWidth           = 0f;
            trail.minVertexDistance  = 0.08f;
            trail.textureMode        = LineTextureMode.Stretch;
            trail.material           = SpriteMat(new Color(0.9f, 0.8f, 0.6f, 0.8f));

            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.9f, 0.8f, 0.55f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.5f),  1f)
                },
                new[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f,    1f)
                });
            trail.colorGradient = grad;
        }

        // ── 工具 ─────────────────────────────────────────────────────
        private static ParticleSystem MakePS(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        // Sprites/Default 在任何 Unity 渲染管線都存在，不會顯示粉紅
        private static Material SpriteMat(Color color, bool additive = false)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            if (additive)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.renderQueue = 3500;
            }
            return mat;
        }

        private static void ApplyMat(ParticleSystem ps, Color tint, bool additive)
        {
            ps.GetComponent<ParticleSystemRenderer>().material = SpriteMat(tint, additive);
        }
    }
}
