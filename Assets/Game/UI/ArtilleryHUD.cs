using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.UI
{
    public class ArtilleryHUD : MonoBehaviour
    {
        private Text _stoneText;
        private Text _ironText;
        private Text _sulfurText;
        private Text _landingText;
        private GameObject _clearedPanel;
        private Text       _clearedText;

        private void OnEnable()
        {
            GameEvents.ResourceChanged += OnResourceChanged;
            GameEvents.LootCollected   += OnLootCollected;
            GameEvents.AreaCleared     += OnAreaCleared;
        }

        private void OnDisable()
        {
            GameEvents.ResourceChanged -= OnResourceChanged;
            GameEvents.LootCollected   -= OnLootCollected;
            GameEvents.AreaCleared     -= OnAreaCleared;
        }

        private void Start()
        {
            EnsureEventSystem();
            BuildHUD();
        }

        private void BuildHUD()
        {
            var root = new GameObject("ArtilleryHUD_Canvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            Font font = GetFont();

            BuildCrosshair(root.transform);
            BuildLandingReadout(root.transform, font);
            BuildAmmoLabel(root.transform, font);
            BuildInventoryPanel(root.transform, font);
            BuildAreaClearedOverlay(root.transform, font);
        }

        // ── 準心下方：落點距離大字 ────────────────────────────────────
        private void BuildLandingReadout(Transform canvas, Font font)
        {
            var go = MakeText("LandingReadout", canvas, "", 36, font,
                new Color(1f, 0.92f, 0.25f), bold: true);
            _landingText = go.GetComponent<Text>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -48f);   // 準心正下方
            rt.sizeDelta = new Vector2(460f, 54f);
        }

        private void Update()
        {
            if (_landingText == null) return;
            bool show = CameraDirector.IsAiming && LandingPreview.LastDistance > 0.5f;
            _landingText.enabled = show;
            if (show)
                _landingText.text = $"LANDING  {LandingPreview.LastDistance:0} m";
        }

        // ── 中央十字準心 ──────────────────────────────────────────────
        private static void BuildCrosshair(Transform canvas)
        {
            Color c   = new Color(1f, 1f, 1f, 0.8f);
            Color dot = new Color(1f, 0.92f, 0f, 1f);
            CrossLine("CH_L",   canvas, new Vector2(-30f, 0f),  new Vector2(20f,  2.5f), c);
            CrossLine("CH_R",   canvas, new Vector2( 30f, 0f),  new Vector2(20f,  2.5f), c);
            CrossLine("CH_U",   canvas, new Vector2(0f,  30f),  new Vector2(2.5f, 20f),  c);
            CrossLine("CH_D",   canvas, new Vector2(0f, -30f),  new Vector2(2.5f, 20f),  c);
            CrossLine("CH_Dot", canvas, Vector2.zero,           new Vector2(6f,   6f),   dot);
        }

        private static void CrossLine(string name, Transform canvas, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas, false);
            go.AddComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        // ── 左下：彈種 ───────────────────────────────────────────────
        private static void BuildAmmoLabel(Transform canvas, Font font)
        {
            var bg = MakePanel("AmmoBG", canvas,
                Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(24f, 24f), new Vector2(260f, 68f),
                new Color(0f, 0f, 0f, 0.55f));

            var txt = MakeText("AmmoType", bg.transform, "HE SHELL", 30, font,
                new Color(1f, 0.85f, 0.35f), bold: true);
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 0f); rt.offsetMax = new Vector2(-8f, 0f);
            txt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        }

        // ── 右上：資源欄（移離右下 FIRE 按鈕，放大字體）─────────────
        private void BuildInventoryPanel(Transform canvas, Font font)
        {
            var bg = MakePanel("InventoryPanel", canvas,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(320f, 168f),
                new Color(0f, 0f, 0f, 0.55f));

            _stoneText  = MakeResLine("StoneRow",  bg.transform, font, "STONE   0",
                new Color(0.78f, 0.78f, 0.82f), 0.66f);
            _ironText   = MakeResLine("IronRow",   bg.transform, font, "IRON    0",
                new Color(0.85f, 0.48f, 0.22f), 0.33f);
            _sulfurText = MakeResLine("SulfurRow", bg.transform, font, "SULFUR  0",
                new Color(0.98f, 0.90f, 0.15f), 0.00f);
        }

        private Text MakeResLine(string name, Transform parent, Font font,
            string label, Color color, float anchorY)
        {
            var go  = MakeText(name, parent, label, 30, font, color, bold: true);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, anchorY);
            rt.anchorMax = new Vector2(1f, anchorY + 0.34f);
            rt.offsetMin = new Vector2(18f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
            go.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            return go.GetComponent<Text>();
        }

        // ── AREA CLEARED 全幕提示 ─────────────────────────────────────
        private void BuildAreaClearedOverlay(Transform canvas, Font font)
        {
            _clearedPanel = MakePanel("AreaCleared", canvas,
                new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.70f));
            _clearedPanel.SetActive(false);

            _clearedText = MakeText("ClearedText", _clearedPanel.transform,
                "AREA CLEARED", 52, font, new Color(1f, 0.88f, 0.2f), bold: true)
                .GetComponent<Text>();
            var rt = _clearedText.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ── 事件處理 ─────────────────────────────────────────────────
        private void OnAreaCleared() => StartCoroutine(DoShowCleared());

        private IEnumerator DoShowCleared()
        {
            if (_clearedPanel) _clearedPanel.SetActive(true);
            yield return new WaitForSeconds(4f);
            if (_clearedPanel) _clearedPanel.SetActive(false);
        }

        private void OnLootCollected(ResourceType type, int amount)
            => RewardPopup.Show(type, amount);

        private void OnResourceChanged(IReadOnlyDictionary<ResourceType, int> res)
        {
            int s = res.TryGetValue(ResourceType.Stone,  out int sv) ? sv : 0;
            int i = res.TryGetValue(ResourceType.Iron,   out int iv) ? iv : 0;
            int u = res.TryGetValue(ResourceType.Sulfur, out int uv) ? uv : 0;
            if (_stoneText)  _stoneText.text  = $"STONE   {s}";
            if (_ironText)   _ironText.text   = $"IRON    {i}";
            if (_sulfurText) _sulfurText.text = $"SULFUR  {u}";
        }

        // ── UI 工廠 ───────────────────────────────────────────────────
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
    }
}
