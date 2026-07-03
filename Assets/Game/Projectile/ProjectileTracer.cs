using UnityEngine;

namespace ArtilleryFrontier.Projectile
{
    /// <summary>
    /// 高可視度砲彈追蹤器。
    /// DefaultExecutionOrder(100)：確保在 ProjectileVFX.Start() 之後執行，
    /// 覆蓋其材質並替換煙霧 Trail 為亮黃色 Tracer Trail。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ProjectileTracer : MonoBehaviour
    {
        private Renderer      _renderer;
        private Material      _coreMat;
        private TrailRenderer _trail;
        private float         _birthTime;

        // 閃動色調
        private static readonly Color ColBright = new Color(1f, 0.95f, 0.05f, 1f);
        private static readonly Color ColDim    = new Color(1f, 0.70f, 0.00f, 0.65f);

        private void Start()
        {
            _birthTime = Time.time;

            // ── 砲彈本體：亮黃 + Additive 混合 ──────────────────────
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _coreMat = MakeAdditiveMat(ColBright);
                _renderer.material = _coreMat;
            }

            // ── 取用 ProjectileVFX 已建立的 TrailRenderer，直接覆寫設定 ──
            // 避免同幀 Destroy + AddComponent：Unity 6 在此窗口內 AddComponent 可能返回 null
            _trail = GetComponent<TrailRenderer>();
            if (_trail == null)
                _trail = gameObject.AddComponent<TrailRenderer>();

            _trail.time              = 0.10f;   // 約 6.2m @ 62 m/s
            _trail.startWidth        = 0.50f;
            _trail.endWidth          = 0.02f;
            _trail.minVertexDistance = 0.04f;
            _trail.textureMode       = LineTextureMode.Stretch;
            _trail.material          = MakeAdditiveMat(new Color(1f, 0.85f, 0.05f));

            // 漸變：頭部亮黃 → 尾部橙紅 → 透明
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.1f), 0f),
                    new GradientColorKey(new Color(1f, 0.50f, 0.0f), 0.6f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0f),  1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f,   1f)
                });
            _trail.colorGradient = grad;
        }

        private void Update()
        {
            if (_coreMat == null) return;
            // 每秒閃動兩次（2 Hz），提升夜間辨識度
            float t = Mathf.Sin((Time.time - _birthTime) * Mathf.PI * 4f) * 0.5f + 0.5f;
            _coreMat.color = Color.Lerp(ColDim, ColBright, t);
        }

        // Sprites/Default + Additive 混合 → 在任何背景上都顯亮（含夜晚）
        private static Material MakeAdditiveMat(Color color)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.color = color;
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.renderQueue = 3500;
            return m;
        }
    }
}
