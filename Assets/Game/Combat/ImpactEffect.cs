using System.Collections;
using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public class ImpactEffect : MonoBehaviour
    {
        private const float Lifetime = 2.5f;

        public static void Spawn(Vector3 position, Vector3 normal)
        {
            var go = new GameObject("ImpactEffect");
            go.transform.position = position;
            go.transform.up = normal;
            go.AddComponent<ImpactEffect>();
        }

        private void Start()
        {
            StartCoroutine(SpawnAll());
            Destroy(gameObject, Lifetime);
        }

        private IEnumerator SpawnAll()
        {
            SpawnFlash();
            SpawnDustCloud();
            yield return null;
            StartCoroutine(SpawnExplosionSphere());
        }

        // ── 閃光（最先亮起）────────────────────────────────────────
        private void SpawnFlash()
        {
            var ps = MakePS("Flash", Vector3.up * 0.1f);
            var m  = ps.main;
            m.duration      = 0.08f;
            m.loop          = false;
            m.startLifetime = 0.12f;
            m.startSpeed    = 0f;
            m.startSize     = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
            m.startColor    = new Color(1f, 0.9f, 0.4f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles  = 5;

            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            ApplyMat(ps, new Color(1f, 0.9f, 0.4f), additive: true);
            ps.Play();
        }

        // ── 煙塵（側視清晰可辨認的主體效果）──────────────────────
        private void SpawnDustCloud()
        {
            var ps = MakePS("Dust", Vector3.zero);
            var m  = ps.main;
            m.duration      = 0.3f;
            m.loop          = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            m.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 7f);
            m.startSize     = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);  // 側視 18m 可見
            m.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.55f, 0.48f, 0.38f, 0.9f),
                new Color(0.72f, 0.65f, 0.52f, 0.6f));
            m.gravityModifier  = -0.05f;
            m.simulationSpace  = ParticleSystemSimulationSpace.World;
            m.maxParticles     = 60;

            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 35) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius    = 0.6f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f, 0.65f, 0.45f), 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.4f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ApplyMat(ps, new Color(0.75f, 0.65f, 0.5f), additive: false);
            ps.Play();
        }

        // ── 爆炸球（衝擊視覺錨點）─────────────────────────────────
        private IEnumerator SpawnExplosionSphere()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ExplosionSphere";
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.up * 0.3f;
            Destroy(sphere.GetComponent<Collider>());

            // Sprites/Default 支援 alpha 透明，在所有管線都可用
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.55f, 0.1f, 0.9f);
            sphere.GetComponent<Renderer>().material = mat;

            const float expandTime = 0.22f;
            const float fadeTime   = 0.55f;
            const float maxRadius  = 2.2f;

            for (float t = 0f; t < expandTime; t += Time.deltaTime)
            {
                sphere.transform.localScale = Vector3.one * Mathf.Lerp(0f, maxRadius, t / expandTime);
                yield return null;
            }

            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                float a = Mathf.Lerp(0.9f, 0f, t / fadeTime);
                mat.color = new Color(1f, 0.55f, 0.1f, a);
                yield return null;
            }

            Destroy(sphere);
        }

        // ── 工具 ─────────────────────────────────────────────────────
        private ParticleSystem MakePS(string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

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
