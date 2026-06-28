using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.UI
{
    public class ArtilleryHUD : MonoBehaviour
    {
        private ArtilleryController _ctrl;

        // Bearing strip（頂部）
        private RectTransform _bearingMarker;
        private RectTransform _bearingLabelRT;
        private Text          _bearingLabelTxt;

        // Elevation gauge（右側）
        private RectTransform _elevMarker;
        private RectTransform _elevLabelRT;
        private Text          _elevLabelTxt;

        // 左下 readout
        private Text _yawReadout;
        private Text _pitchReadout;

        private const float MaxPitch = 80f;

        private void Start()
        {
            _ctrl = FindFirstObjectByType<ArtilleryController>();
            if (_ctrl == null) { enabled = false; return; }
            EnsureEventSystem();
            BuildHUD();
        }

        private void LateUpdate()
        {
            float yaw   = _ctrl.GetTargetYaw();
            float pitch = _ctrl.GetTargetPitch();

            // Bearing marker 橫移（-90→左端, +90→右端）
            float ny = (yaw + 90f) / 180f;
            SetAnchorX(_bearingMarker,  ny, new Vector2(3f,  -8f));
            SetAnchorX(_bearingLabelRT, ny, new Vector2(52f,  22f));
            _bearingLabelTxt.text = $"{yaw:+0;-0;0}°";

            // Elevation marker 縱移（0→底, 80→頂）
            float np = pitch / MaxPitch;
            SetAnchorY(_elevMarker,  np, new Vector2(-12f, 4f));
            SetAnchorY(_elevLabelRT, np, new Vector2(36f,  22f));
            _elevLabelTxt.text = $"{pitch:0}°";

            _yawReadout.text   = $"YAW {yaw:+0.0;-0.0; 0.0}°";
            _pitchReadout.text = $"ELV {pitch:0.0}°";
        }

        // ── anchor 工具 ────────────────────────────────────────────────
        private static void SetAnchorX(RectTransform rt, float nx, Vector2 size)
        {
            rt.anchorMin = new Vector2(nx, 0f);
            rt.anchorMax = new Vector2(nx, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchorY(RectTransform rt, float ny, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, ny);
            rt.anchorMax = new Vector2(1f, ny);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        // ══════════════════════════════════════════════════════════════
        // 建構 HUD
        // ══════════════════════════════════════════════════════════════
        private void BuildHUD()
        {
            var root = new GameObject("ArtilleryHUD_Canvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight   = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            Font font = GetFont();

            var bearingBG = BuildBearingStrip(root.transform, font);
            var elevBG    = BuildElevationGauge(root.transform, font);
            BuildReadout(root.transform, font);
            BuildCrosshair(root.transform);

            // ── 觸控互動層（透明 Image 覆蓋在刻度條上） ─────────────
            AddGaugeInteract(bearingBG, isHorizontal: true);
            AddGaugeInteract(elevBG,   isHorizontal: false);
        }

        // ── Bearing Strip ─────────────────────────────────────────────
        private GameObject BuildBearingStrip(Transform canvas, Font font)
        {
            var bg = MakePanel("BearingStrip", canvas,
                new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 52f),
                new Color(0f, 0f, 0f, 0.55f));

            int[] ticks = { -90, -60, -30, 0, 30, 60, 90 };
            foreach (int deg in ticks)
            {
                float t     = (deg + 90f) / 180f;
                bool major  = deg == 0 || Mathf.Abs(deg) == 90;
                float tw    = major ? 2.5f : 1.5f;
                Color tc    = major ? Color.white : new Color(1f, 1f, 1f, 0.4f);

                // 刻度線
                MakePanel($"T{deg}", bg.transform,
                    new Vector2(t, 0.1f), new Vector2(t, 0.9f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(tw, 0f), tc);

                // 靜態文字
                var lbl = MakeText($"L{deg}", bg.transform, $"{deg}°", 13, font,
                    new Color(1f, 1f, 1f, major ? 0.85f : 0.4f));
                var lr = lbl.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(t, 0f);
                lr.anchorMax = new Vector2(t, 0f);
                lr.pivot     = new Vector2(0.5f, 0f);
                lr.sizeDelta = new Vector2(44f, 20f);
                lr.anchoredPosition = new Vector2(0f, 3f);
            }

            // 中央固定參考線
            MakePanel("CenterRef", bg.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1.5f, 0f), new Color(1f, 1f, 1f, 0.18f));

            // 移動 marker（亮黃）
            var markerGO = MakePanel("BearingMarker", bg.transform,
                Vector2.zero, Vector2.zero, Vector2.one * 0.5f,
                Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0f, 1f));
            _bearingMarker = markerGO.GetComponent<RectTransform>();

            // 移動數值標籤
            var lblGO = MakeText("BearingValue", bg.transform, "0°", 15, font,
                new Color(1f, 0.92f, 0f, 1f), bold: true);
            _bearingLabelRT  = lblGO.GetComponent<RectTransform>();
            _bearingLabelTxt = lblGO.GetComponent<Text>();
            _bearingLabelRT.pivot = new Vector2(0.5f, 1f);

            return bg;
        }

        // ── Elevation Gauge ────────────────────────────────────────────
        private GameObject BuildElevationGauge(Transform canvas, Font font)
        {
            var bg = MakePanel("ElevGauge", canvas,
                new Vector2(1f, 0.25f), new Vector2(1f, 0.75f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(52f, 0f),
                new Color(0f, 0f, 0f, 0.55f));

            // 軌道線
            MakePanel("ElevTrack", bg.transform,
                new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(2f, 0f), new Color(1f, 1f, 1f, 0.22f));

            int[] ticks = { 0, 20, 40, 60, 80 };
            foreach (int deg in ticks)
            {
                float t     = (float)deg / MaxPitch;
                bool major  = deg == 0 || deg == 80;

                MakePanel($"ET{deg}", bg.transform,
                    new Vector2(0.1f, t), new Vector2(0.9f, t), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(0f, major ? 2f : 1.5f),
                    major ? Color.white : new Color(1f, 1f, 1f, 0.38f));

                var lbl = MakeText($"EL{deg}", bg.transform, $"{deg}°", 12, font,
                    new Color(1f, 1f, 1f, major ? 0.85f : 0.4f));
                var lr = lbl.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(0f, t);
                lr.anchorMax = new Vector2(0f, t);
                lr.pivot     = new Vector2(0f, 0.5f);
                lr.sizeDelta = new Vector2(26f, 20f);
                lr.anchoredPosition = new Vector2(3f, 0f);
            }

            // 移動 marker（亮黃）
            var markerGO = MakePanel("ElevMarker", bg.transform,
                Vector2.zero, Vector2.zero, Vector2.one * 0.5f,
                Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0f, 1f));
            _elevMarker = markerGO.GetComponent<RectTransform>();

            // 移動數值標籤
            var lblGO = MakeText("ElevValue", bg.transform, "0°", 14, font,
                new Color(1f, 0.92f, 0f, 1f), bold: true);
            _elevLabelRT  = lblGO.GetComponent<RectTransform>();
            _elevLabelTxt = lblGO.GetComponent<Text>();
            _elevLabelRT.pivot = new Vector2(0f, 0.5f);

            return bg;
        }

        // ── 觸控互動層（覆蓋刻度條，透明但可 raycast）────────────────
        private void AddGaugeInteract(GameObject gaugeRoot, bool isHorizontal)
        {
            var overlay = new GameObject("Interact");
            overlay.transform.SetParent(gaugeRoot.transform, false);

            var rt = overlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Color.clear 但仍是 raycast target
            var img = overlay.AddComponent<Image>();
            img.color = Color.clear;

            var input = overlay.AddComponent<HUDGaugeInput>();
            input.Ctrl         = _ctrl;
            input.IsHorizontal = isHorizontal;
        }

        // ── 左下 Readout ───────────────────────────────────────────────
        private void BuildReadout(Transform canvas, Font font)
        {
            var bg = MakePanel("Readout", canvas,
                Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(16f, 16f), new Vector2(175f, 64f),
                new Color(0f, 0f, 0f, 0.58f));

            // 頂部黃線
            MakePanel("Border", bg.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 2.5f), new Color(1f, 0.88f, 0f, 0.85f));

            _yawReadout   = BuildReadoutLine("YawLine",   bg.transform, "YAW  0.0°",
                Vector2.zero,         new Vector2(0f, 0.5f), new Vector2(1f, 1f), font,
                new Color(1f, 0.9f, 0.4f, 1f));
            _pitchReadout = BuildReadoutLine("PitchLine", bg.transform, "ELV  0.0°",
                Vector2.zero,         new Vector2(0f, 0f),   new Vector2(1f, 0.5f), font,
                new Color(0.45f, 1f, 0.55f, 1f));
        }

        private Text BuildReadoutLine(string name, Transform parent, string content,
            Vector2 pivot, Vector2 aMin, Vector2 aMax, Font font, Color color)
        {
            var go = MakeText(name, parent, content, 18, font, color, bold: true);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot     = pivot;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);
            var txt = go.GetComponent<Text>();
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        // ── 中央十字準心 ──────────────────────────────────────────────
        private void BuildCrosshair(Transform canvas)
        {
            Color c   = new Color(1f, 1f, 1f, 0.8f);
            Color dot = new Color(1f, 0.92f, 0f, 1f);
            CrossLine("CH_L",   canvas, new Vector2(-18f, 0f),  new Vector2(12f,  1.5f), c);
            CrossLine("CH_R",   canvas, new Vector2( 18f, 0f),  new Vector2(12f,  1.5f), c);
            CrossLine("CH_U",   canvas, new Vector2(0f,  18f),  new Vector2(1.5f, 12f),  c);
            CrossLine("CH_D",   canvas, new Vector2(0f, -18f),  new Vector2(1.5f, 12f),  c);
            CrossLine("CH_Dot", canvas, Vector2.zero,           new Vector2(4f,   4f),   dot);
        }

        private static void CrossLine(string name, Transform canvas, Vector2 pos, Vector2 size, Color color)
        {
            MakePanel(name, canvas,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, size, color);
        }

        // ── EventSystem（UI 觸控 / 點擊所需）─────────────────────────
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // ══════════════════════════════════════════════════════════════
        // UI 工廠
        // ══════════════════════════════════════════════════════════════
        private static GameObject MakePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject MakeText(string name, Transform parent, string content,
            int size, Font font, Color color, bool bold = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.fontSize  = size;
            txt.color     = color;
            txt.font      = font;
            txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            txt.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        private static Font GetFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
