using System.Collections.Generic;
using UnityEngine;

namespace ArtilleryFrontier.Projectile
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private int stepCount = 90;
        [SerializeField] private float timeStep = 0.05f;
        [SerializeField] private float updateInterval = 0.04f;

        [Header("Range")]
        [SerializeField] private float maxRange = 220f;

        [Header("Debug")]
        [SerializeField] private bool showTrajectoryAlways = false;

        [Header("Wind (reserved)")]
        [SerializeField] private Vector3 windForce = Vector3.zero;

        [Header("References")]
        [SerializeField] private ProjectileLauncher launcher;

        // ── 線條顏色 ──────────────────────────────────────────────────
        // 起點（亮黃）→ 中段（橘黃）→ 末端（橘紅 fade）
        private static readonly Color C_StartYellow = new Color(1f,  0.98f, 0.05f, 1f);
        private static readonly Color C_MidOrange   = new Color(1f,  0.62f, 0.05f, 0.88f);
        private static readonly Color C_EndRed      = new Color(1f,  0.15f, 0.03f, 0.25f);
        private static readonly Color C_RedBright   = new Color(1f,  0.18f, 0.04f, 1f);

        private LineRenderer _line;
        private LineRenderer _endRing;
        private float _nextUpdateTime;

        // 落點
        private Vector3 _impactPos;
        private bool _impactBeyondRange;

        // Ring pulse
        private float _pulseT;
        private const float PulseSpeed = 2.8f;
        private const float RingBaseRadius = 1.4f;
        private const float RingPulseAmp   = 0.35f;
        private const float RingBaseWidth  = 0.10f;
        private const float RingPulseWidth = 0.08f;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            SetupMainLine();
            SetupEndRing();
        }

        private void SetupMainLine()
        {
            _line.positionCount = stepCount;
            _line.startWidth = 0.18f;   // 明顯加粗
            _line.endWidth   = 0.06f;
            _line.useWorldSpace = true;
            _line.textureMode = LineTextureMode.Tile;
            _line.sortingOrder = 15;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            // Sprites/Default：支援頂點色漸層 + 高 renderQueue（覆蓋地形）
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 4000;
            _line.material = mat;
        }

        private void SetupEndRing()
        {
            var ringGO = new GameObject("EndRing");
            ringGO.transform.SetParent(transform);

            _endRing = ringGO.AddComponent<LineRenderer>();
            _endRing.positionCount = 49;   // 48 段 + 1（loop 重合首尾）
            _endRing.loop = true;
            _endRing.useWorldSpace = true;
            _endRing.sortingOrder = 16;
            _endRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _endRing.receiveShadows = false;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 4000;
            _endRing.material = mat;
        }

        private void Update()
        {
            if (!showTrajectoryAlways)
            {
                SetVisible(false);
                return;
            }

            _pulseT += Time.deltaTime * PulseSpeed;
            UpdateRingPulse();

            if (Time.time >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.time + updateInterval;
                if (launcher != null) Simulate();
            }
        }

        // ── 彈道模擬 ─────────────────────────────────────────────────
        private void Simulate()
        {
            Transform muzzle = launcher.GetMuzzle();
            Vector3 startPos = muzzle.position;
            Vector3 vel      = muzzle.forward * launcher.GetSpeed();
            float gravMult   = launcher.GetGravityMultiplier();
            Vector3 accel    = Physics.gravity * gravMult + windForce;

            var points   = new List<Vector3>(stepCount);
            var prevPos  = startPos;
            var pos      = startPos;
            int overRangeIdx = -1;  // 首次超距的 index
            _impactPos = startPos;

            for (int i = 0; i < stepCount; i++)
            {
                float terrainY = SampleTerrainHeight(pos);

                // 落地偵測（精確插值）
                if (i > 0 && pos.y <= terrainY)
                {
                    float t = Mathf.Clamp01((prevPos.y - terrainY) / Mathf.Max(0.001f, prevPos.y - pos.y));
                    _impactPos = Vector3.Lerp(prevPos, pos, t);
                    _impactPos.y = SampleTerrainHeight(_impactPos) + 0.05f;
                    points.Add(_impactPos);
                    break;
                }

                // 超出地圖下限
                if (pos.y < -20f)
                {
                    _impactPos = new Vector3(pos.x, terrainY + 0.05f, pos.z);
                    points.Add(_impactPos);
                    break;
                }

                // 記錄首次超距位置
                float dist = Vector3.Distance(startPos, pos);
                if (overRangeIdx < 0 && dist > maxRange) overRangeIdx = i;

                // 顯示位置貼地（避免線條被地形吃掉）
                float dispY = Mathf.Max(pos.y, terrainY + 0.18f);
                points.Add(new Vector3(pos.x, dispY, pos.z));

                _impactPos = pos;
                prevPos = pos;
                vel += accel * timeStep;
                pos += vel * timeStep;
            }

            // 套用到 LineRenderer
            _line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                _line.SetPosition(i, points[i]);

            // Gradient：亮黃 → 橘 → 橘紅 fade；超距後段改紅
            _line.colorGradient = BuildGradient(points.Count, overRangeIdx);

            _impactBeyondRange = overRangeIdx >= 0;
            SetRingPositions(_impactPos, RingBaseRadius);
        }

        // ── 漸層建構 ─────────────────────────────────────────────────
        private Gradient BuildGradient(int totalPoints, int overRangeIdx)
        {
            var grad = new Gradient();

            if (overRangeIdx < 0)
            {
                // 全程在射程內：亮黃 → 橘 → 橘紅 fade
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(C_StartYellow, 0f),
                        new GradientColorKey(C_MidOrange,   0.5f),
                        new GradientColorKey(C_EndRed,      1f)
                    },
                    new[] {
                        new GradientAlphaKey(1f,   0f),
                        new GradientAlphaKey(0.85f, 0.5f),
                        new GradientAlphaKey(0.2f,  1f)
                    });
            }
            else
            {
                // 超距：黃段 + 紅段
                float splitT = Mathf.Clamp01((float)overRangeIdx / Mathf.Max(1, totalPoints - 1));
                float splitT1 = Mathf.Min(splitT + 0.001f, 1f);
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(C_StartYellow, 0f),
                        new GradientColorKey(C_MidOrange,   splitT),
                        new GradientColorKey(C_RedBright,   splitT1),
                        new GradientColorKey(C_EndRed,      1f)
                    },
                    new[] {
                        new GradientAlphaKey(1f,   0f),
                        new GradientAlphaKey(0.85f, splitT),
                        new GradientAlphaKey(0.9f,  splitT1),
                        new GradientAlphaKey(0.2f,  1f)
                    });
            }

            return grad;
        }

        // ── 落點圓圈：位置 ───────────────────────────────────────────
        private void SetRingPositions(Vector3 center, float radius)
        {
            int seg = _endRing.positionCount - 1;
            float groundY = SampleTerrainHeight(center) + 0.06f;
            center.y = Mathf.Max(center.y, groundY);

            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                _endRing.SetPosition(i, center + new Vector3(
                    Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        // ── 落點圓圈：Pulse 動畫（每幀）────────────────────────────
        private void UpdateRingPulse()
        {
            float pulse = Mathf.Sin(_pulseT * Mathf.PI) * 0.5f + 0.5f;  // 0~1 週期

            float radius = RingBaseRadius + pulse * RingPulseAmp;
            float width  = RingBaseWidth  + pulse * RingPulseWidth;
            _endRing.startWidth = width;
            _endRing.endWidth   = width;

            // 重設半徑
            SetRingPositions(_impactPos, radius);

            // 顏色：超距紅色 / 否則橘紅，pulse 影響 alpha
            Color baseColor = _impactBeyondRange
                ? new Color(1f, 0.1f, 0.05f, 1f)
                : new Color(1f, 0.3f, 0.05f, 1f);

            float alpha = Mathf.Lerp(0.5f, 1f, pulse);
            _endRing.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            _endRing.endColor   = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.35f);
        }

        // ── Terrain 採樣 ─────────────────────────────────────────────
        private static float SampleTerrainHeight(Vector3 worldPos)
        {
            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain.SampleHeight(worldPos)
                     + Terrain.activeTerrain.transform.position.y;
            return 0f;
        }

        public void SetWind(Vector3 wind) => windForce = wind;

        public void SetVisible(bool visible)
        {
            if (_line    != null) _line.enabled    = visible;
            if (_endRing != null) _endRing.enabled = visible;
        }
    }
}
