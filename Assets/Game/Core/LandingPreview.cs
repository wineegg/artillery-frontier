using UnityEngine;
using UnityEngine.UI;
using ArtilleryFrontier.Projectile;

namespace ArtilleryFrontier.Core
{
    public class LandingPreview : MonoBehaviour
    {
        // 供 ObservationMode.Enter() / HUD 讀取最近一次有效落點
        public static Vector3 LastPoint      { get; private set; }
        public static float   LastFlightTime { get; private set; }
        public static float   LastDistance   { get; private set; }   // 砲口→落點水平/直線距離 (m)；0 = 無效

        private LineRenderer       _ring;
        private LineRenderer       _pillar;
        private Canvas             _canvas;
        private Text               _label;
        private ProjectileLauncher _launcher;

        private const int   Segs    = 48;
        private const float BaseRad = 12f;   // 圈半徑 12m
        private const float PulseHz = 2.5f;

        private void Start()
        {
            BuildRing();
            BuildPillar();
            BuildLabel();
        }

        private void LateUpdate()
        {
            bool show = CameraDirector.IsAiming;
            _ring.enabled   = show;
            _pillar.enabled = show;
            _canvas.gameObject.SetActive(show);
            if (!show) return;

            if (_launcher == null)
                _launcher = FindAnyObjectByType<ProjectileLauncher>();
            if (_launcher == null) return;

            float flightTime;
            Vector3 landing = SimulateLanding(out flightTime);

            // 砲管幾乎朝上（彈道不落地）→ 隱藏並清空快取
            if (flightTime >= GameConfig.SimMaxTime - 0.01f)
            {
                LastPoint = Vector3.zero;
                LastDistance = 0f;
                _ring.enabled   = false;
                _pillar.enabled = false;
                _canvas.gameObject.SetActive(false);
                return;
            }

            // 更新靜態快取供 ObservationMode 使用
            LastPoint      = landing;
            LastFlightTime = flightTime;

            // 脈動
            float t     = Time.time;
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * PulseHz) * 0.15f;
            float alpha = Mathf.Sin(t * Mathf.PI * PulseHz) * 0.25f + 0.75f;
            Color col   = new Color(1f, 0.05f, 0.05f, alpha);
            _ring.startColor = _ring.endColor = col;

            // 更新圈
            float r = BaseRad * pulse;
            for (int i = 0; i < Segs; i++)
            {
                float a = i / (float)Segs * Mathf.PI * 2f;
                _ring.SetPosition(i, landing + new Vector3(
                    Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r));
            }

            // 更新垂直光柱
            Color pillarCol = new Color(1f, 0.05f, 0.05f, alpha * 0.6f);
            _pillar.startColor = _pillar.endColor = pillarCol;
            _pillar.SetPosition(0, landing + Vector3.up * 0.2f);
            _pillar.SetPosition(1, landing + Vector3.up * 20f);

            // 更新標籤
            float dist = Vector3.Distance(_launcher.GetMuzzle().position, landing);
            LastDistance = dist;
            _label.text = $"DIST  {dist:0} m\nTIME  {flightTime:0.0} s";
            _canvas.transform.position = landing + Vector3.up * 22f;

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 d = cam.transform.position - _canvas.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f)
                    _canvas.transform.rotation = Quaternion.LookRotation(-d);
            }
        }

        private Vector3 SimulateLanding(out float flightTime)
        {
            Transform muzzle = _launcher.GetMuzzle();

            // 與 Projectile 共用同一套 Ballistics 與相同步長（Time.fixedDeltaTime）
            // → 預測落點 = 實際落點。
            var res = Ballistics.Predict(
                muzzle.position,
                muzzle.forward * GameConfig.MuzzleSpeed,
                Time.fixedDeltaTime,
                GameConfig.SimMaxTime);

            flightTime = res.grounded ? res.time : GameConfig.SimMaxTime;
            Vector3 p  = res.point;
            p.y += 0.2f;   // 抬離地面避免圈與地形 z-fighting
            return p;
        }

        private void BuildRing()
        {
            var go = new GameObject("LandingRing");
            go.transform.SetParent(transform);
            _ring = go.AddComponent<LineRenderer>();
            _ring.loop          = true;
            _ring.positionCount = Segs;
            _ring.startWidth    = 0.3f;
            _ring.endWidth      = 0.3f;
            _ring.useWorldSpace = true;
            _ring.material      = SpriteMat(new Color(1f, 0.05f, 0.05f, 0.9f));
        }

        private void BuildPillar()
        {
            var go = new GameObject("LandingPillar");
            go.transform.SetParent(transform);
            _pillar = go.AddComponent<LineRenderer>();
            _pillar.positionCount = 2;
            _pillar.startWidth    = 0.5f;
            _pillar.endWidth      = 0.05f;
            _pillar.useWorldSpace = true;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.05f, 0.05f, 0.7f);
            // Additive blending for glow effect
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3500;
            _pillar.material = mat;
        }

        private void BuildLabel()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject("LandingLabel");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.WorldSpace;
            _canvas.sortingOrder = 8;
            var crt = go.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(220f, 110f);
            go.transform.localScale = Vector3.one * 0.10f;

            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var tGO = new GameObject("Text");
            tGO.transform.SetParent(go.transform, false);
            _label = tGO.AddComponent<Text>();
            _label.font      = font;
            _label.fontSize  = 46;
            _label.fontStyle = FontStyle.Bold;
            _label.color     = new Color(1f, 0.25f, 0.25f);
            _label.alignment = TextAnchor.MiddleCenter;
            var trt = tGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        private static Material SpriteMat(Color c)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.color = c;
            return m;
        }
    }
}
