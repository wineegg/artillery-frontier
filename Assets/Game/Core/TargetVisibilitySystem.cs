using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Projectile;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 螢幕空間目標標記（可點擊）。每個目標一個固定大小標記（名稱 + 距離），
    /// 在畫面內 → 標其位置；離開畫面 → 貼邊緣並以箭頭指向。
    /// 點擊標記 = 選定該目標並自動瞄準（彈道解算），選中者高亮。
    /// </summary>
    public class TargetVisibilitySystem : MonoBehaviour
    {
        private class Marker
        {
            public DestructibleTarget target;
            public RectTransform      root;
            public Image              bg;
            public Text               label;
            public Image              icon;
            public RectTransform      arrow;
        }

        private readonly List<Marker> _markers = new();
        private RectTransform     _canvasRect;
        private Transform         _refPoint;
        private ArtilleryController _artillery;
        private Marker            _selected;
        private Font              _font;

        private const float EdgePad = 64f;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var launcher = FindAnyObjectByType<ProjectileLauncher>();
            _refPoint  = launcher != null ? launcher.GetMuzzle() : transform;
            _artillery = FindAnyObjectByType<ArtilleryController>();

            EnsureEventSystem();
            BuildCanvas();

            foreach (var dt in FindObjectsByType<DestructibleTarget>(FindObjectsSortMode.None))
                _markers.Add(CreateMarker(dt));
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            float   maxX = screenCenter.x - EdgePad;
            float   maxY = screenCenter.y - EdgePad;
            Vector3 refPos = _refPoint != null ? _refPoint.position : cam.transform.position;

            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                var m = _markers[i];
                if (m.target == null)
                {
                    if (_selected == m) _selected = null;
                    Destroy(m.root.gameObject);
                    _markers.RemoveAt(i);
                    continue;
                }

                Vector3 sp     = cam.WorldToScreenPoint(TargetTop(m.target));
                bool    behind = sp.z < 0f;

                Vector2 screenPos = new Vector2(sp.x, sp.y);
                if (behind) screenPos = screenCenter - (screenPos - screenCenter);

                Vector2 dir = screenPos - screenCenter;
                bool onScreen = !behind
                    && screenPos.x >= EdgePad && screenPos.x <= Screen.width  - EdgePad
                    && screenPos.y >= EdgePad && screenPos.y <= Screen.height - EdgePad;

                Vector2 finalScreen;
                if (onScreen)
                {
                    finalScreen = screenPos;
                    m.icon.gameObject.SetActive(true);
                    m.arrow.gameObject.SetActive(false);
                }
                else
                {
                    if (dir.sqrMagnitude < 1f) dir = Vector2.down;
                    float scale = Mathf.Min(maxX / Mathf.Max(Mathf.Abs(dir.x), 0.0001f),
                                            maxY / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
                    finalScreen = screenCenter + dir * scale;
                    m.icon.gameObject.SetActive(false);
                    m.arrow.gameObject.SetActive(true);
                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    m.arrow.localRotation = Quaternion.Euler(0f, 0f, ang - 90f);
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, finalScreen, null, out Vector2 local);
                m.root.anchoredPosition = local;

                float dist = Vector3.Distance(refPos, m.target.transform.position);
                m.label.text = $"{ShortName(m.target.name)}\n{dist:0} m";

                // 選中高亮
                bool sel = (m == _selected);
                m.bg.color       = sel ? new Color(0.55f, 0.42f, 0.02f, 0.82f)
                                       : new Color(0f, 0f, 0f, 0.6f);
                m.label.color    = sel ? new Color(1f, 0.96f, 0.4f) : Color.white;
                m.root.localScale = Vector3.one * (sel ? 1.18f : 1f);
            }
        }

        private void OnMarkerClicked(Marker m)
        {
            if (m.target == null) return;
            _selected = m;
            if (_artillery == null) _artillery = FindAnyObjectByType<ArtilleryController>();
            _artillery?.AimAtTarget(m.target.transform.position);
        }

        // ── 建構 ─────────────────────────────────────────────────────
        private void BuildCanvas()
        {
            var go = new GameObject("TargetMarkerCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            _canvasRect = go.GetComponent<RectTransform>();
        }

        private Marker CreateMarker(DestructibleTarget dt)
        {
            Color col = TargetColor(dt);
            var marker = new Marker { target = dt };

            var root = new GameObject($"Marker_{dt.name}");
            root.transform.SetParent(_canvasRect, false);
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(180f, 120f);
            marker.root = rootRT;

            // 點擊區（透明、可 raycast）+ Button
            var click = root.AddComponent<Image>();
            click.color = new Color(0f, 0f, 0f, 0f);
            click.raycastTarget = true;
            var btn = root.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var captured = marker;
            btn.onClick.AddListener(() => OnMarkerClicked(captured));

            // 深色底
            marker.bg = AddChildImage(root.transform, "Bg", new Color(0f, 0f, 0f, 0.6f),
                new Vector2(0f, -8f), new Vector2(156f, 62f));
            marker.bg.raycastTarget = false;

            // 名稱 + 距離
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(root.transform, false);
            var label = lblGO.AddComponent<Text>();
            label.font      = _font;
            label.fontSize  = 26;
            label.fontStyle = FontStyle.Bold;
            label.color     = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget      = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow   = VerticalWrapMode.Overflow;
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.sizeDelta        = new Vector2(156f, 62f);
            lblRT.anchoredPosition = new Vector2(0f, -8f);
            marker.label = label;

            // 方框圖示（畫面內時顯示）
            marker.icon = AddChildImage(root.transform, "Icon", col,
                new Vector2(0f, 34f), new Vector2(24f, 24f));
            marker.icon.raycastTarget = false;

            // 邊緣箭頭（離屏時顯示）
            var arrGO = new GameObject("Arrow");
            arrGO.transform.SetParent(root.transform, false);
            var arrow = arrGO.AddComponent<Text>();
            arrow.font      = _font;
            arrow.fontSize  = 40;
            arrow.text      = "▲";
            arrow.color     = col;
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.raycastTarget      = false;
            arrow.horizontalOverflow = HorizontalWrapMode.Overflow;
            arrow.verticalOverflow   = VerticalWrapMode.Overflow;
            var arrRT = arrGO.GetComponent<RectTransform>();
            arrRT.sizeDelta        = new Vector2(48f, 48f);
            arrRT.anchoredPosition = new Vector2(0f, 34f);
            arrGO.SetActive(false);
            marker.arrow = arrRT;

            return marker;
        }

        private static Image AddChildImage(Transform parent, string name, Color color,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        // ── 工具 ─────────────────────────────────────────────────────
        private static Vector3 TargetTop(DestructibleTarget dt)
        {
            float maxY = dt.transform.position.y;
            foreach (var r in dt.GetComponentsInChildren<Renderer>())
                if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
            return new Vector3(dt.transform.position.x, maxY + 3f, dt.transform.position.z);
        }

        private static Color TargetColor(DestructibleTarget dt)
        {
            if (dt is ResourceNode rn)
            {
                return rn.NodeType switch
                {
                    ResourceType.Stone  => new Color(0.80f, 0.80f, 0.85f),
                    ResourceType.Iron   => new Color(0.35f, 0.55f, 0.95f),
                    ResourceType.Sulfur => new Color(1f,    0.90f, 0.15f),
                    _                   => Color.white,
                };
            }
            return new Color(0.95f, 0.55f, 0.35f);   // 城堡：橙
        }

        private static string ShortName(string raw)
        {
            int u = raw.IndexOf('_');
            return (u >= 0 ? raw.Substring(0, u) : raw).ToUpper();
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
