using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 底部彈種選擇列（d.png 風格）+ 數字鍵 1-5 切換。ProjectileLauncher 讀 Current。
    /// </summary>
    public class AmmoSelector : MonoBehaviour
    {
        public static AmmoType Current { get; private set; } = AmmoType.Normal;

        private Image[] _cells;
        private int     _selected;

        private void Awake() => Current = AmmoType.Normal;

        private void Start()
        {
            BuildUI();
            Select(0);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if      (kb.digit1Key.wasPressedThisFrame) Select(0);
            else if (kb.digit2Key.wasPressedThisFrame) Select(1);
            else if (kb.digit3Key.wasPressedThisFrame) Select(2);
            else if (kb.digit4Key.wasPressedThisFrame) Select(3);
            else if (kb.digit5Key.wasPressedThisFrame) Select(4);
        }

        private void Select(int i)
        {
            _selected = Mathf.Clamp(i, 0, AmmoConfig.Order.Length - 1);
            Current   = AmmoConfig.Order[_selected];
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            if (_cells == null) return;
            for (int i = 0; i < _cells.Length; i++)
            {
                Color c = AmmoConfig.Get(AmmoConfig.Order[i]).color;
                _cells[i].color = (i == _selected)
                    ? c
                    : new Color(c.r * 0.32f, c.g * 0.32f, c.b * 0.32f, 0.8f);
            }
        }

        private void BuildUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGO = new GameObject("AmmoBarCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            int   n     = AmmoConfig.Order.Length;
            float cellW = 118f, cellH = 72f, gap = 12f;
            float total = n * cellW + (n - 1) * gap;
            float startX = -total * 0.5f + cellW * 0.5f;

            _cells = new Image[n];
            for (int i = 0; i < n; i++)
            {
                var spec = AmmoConfig.Get(AmmoConfig.Order[i]);

                var cellGO = new GameObject($"Ammo{i}");
                cellGO.transform.SetParent(canvasGO.transform, false);
                var img = cellGO.AddComponent<Image>();
                var rt  = cellGO.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot            = new Vector2(0.5f, 0f);
                rt.sizeDelta        = new Vector2(cellW, cellH);
                rt.anchoredPosition = new Vector2(startX + i * (cellW + gap), 34f);

                var btn = cellGO.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() => Select(idx));
                _cells[i] = img;

                var txtGO = new GameObject("Label");
                txtGO.transform.SetParent(cellGO.transform, false);
                var t = txtGO.AddComponent<Text>();
                t.font = font; t.fontSize = 22; t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
                t.raycastTarget = false;
                t.text = $"{i + 1}\n{spec.name}";
                var trt = txtGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
        }
    }
}
