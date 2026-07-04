using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Projectile;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 螢幕空間敵人標記（只標敵人）。以「抵達基地時間 ETA = 距基地/速度」判斷危急度
    /// （含速度差異），用顏色編碼威脅、標出最急的一隻。
    /// 畫面內：名稱 + 距離 + 血條；離屏：放大的方向箭頭 + 距離，顏色/大小隨威脅。
    /// 點擊 = 鎖定並自動瞄準。SelectedTarget 供 HUD 顯示可發射狀態。
    /// </summary>
    public class TargetVisibilitySystem : MonoBehaviour
    {
        public static DestructibleTarget SelectedTarget { get; private set; }

        private class Marker
        {
            public DestructibleTarget target;
            public RectTransform      root;
            public GameObject         onScreen;
            public GameObject         offScreen;
            public Image              bg;
            public Text               label;
            public RectTransform      hpFillRT;
            public Image              hpFillImg;
            public RectTransform      arrow;
            public Text               arrowTxt;
            public Text               arrowDist;
        }

        private readonly List<Marker> _markers = new();
        private RectTransform       _canvasRect;
        private Transform           _refPoint;
        private ArtilleryController _artillery;
        private Marker              _selected;
        private Font                _font;

        private const float EdgePad    = 64f;
        private const float DangerEta  = 8f;    // 秒
        private const float WarnEta    = 16f;

        private static readonly Color ColDanger = new Color(1f,   0.28f, 0.22f);
        private static readonly Color ColWarn   = new Color(1f,   0.82f, 0.20f);
        private static readonly Color ColSafe   = new Color(0.65f,0.92f, 0.65f);

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var launcher = FindAnyObjectByType<ProjectileLauncher>();
            _refPoint  = launcher != null ? launcher.GetMuzzle() : transform;
            _artillery = FindAnyObjectByType<ArtilleryController>();

            EnsureEventSystem();
            BuildCanvas();
            GameEvents.EnemySpawned += OnEnemySpawned;
        }

        private void OnDestroy()
        {
            GameEvents.EnemySpawned -= OnEnemySpawned;
            SelectedTarget = null;
        }

        private void OnEnemySpawned(DestructibleTarget enemy)
        {
            if (enemy != null && _canvasRect != null)
                _markers.Add(CreateMarker(enemy));
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // 鎖定：連續解算提前量；手動拖曳解除
            if (_artillery != null && _artillery.ManualAimActive) _selected = null;
            if (_selected != null && _selected.target != null && _artillery != null)
            {
                Vector3 lockVel = _selected.target is Enemy le ? le.Velocity : Vector3.zero;
                _artillery.AimAtTarget(_selected.target.transform.position, lockVel);
            }
            SelectedTarget = (_selected != null) ? _selected.target : null;

            // 前置：找出最危急的一隻（ETA 最小）
            Marker top = null; float minEta = float.MaxValue;
            foreach (var m in _markers)
            {
                if (m.target == null) continue;
                float eta = Eta(m.target);
                if (eta < minEta) { minEta = eta; top = m; }
            }

            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            float   maxX = screenCenter.x - EdgePad;
            float   maxY = screenCenter.y - EdgePad;
            float   pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.12f;

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

                float eta      = Eta(m.target);
                Color threat    = ThreatColor(eta);
                bool  isTop     = (m == top);
                bool  isSel     = (m == _selected);
                float distBase  = new Vector2(m.target.transform.position.x,
                                              m.target.transform.position.z).magnitude;

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
                    m.onScreen.SetActive(true);
                    m.offScreen.SetActive(false);

                    m.label.text  = isTop ? $"⚠ {ShortName(m.target.name)}  {distBase:0}m"
                                          : $"{ShortName(m.target.name)}  {distBase:0}m";
                    m.label.color = threat;

                    float hpRatio = m.target.GetMaxHP() > 0f
                        ? Mathf.Clamp01(m.target.GetHP() / m.target.GetMaxHP()) : 0f;
                    m.hpFillRT.anchorMax = new Vector2(hpRatio, 1f);
                    m.hpFillImg.color = hpRatio > 0.5f ? new Color(0.35f, 0.85f, 0.35f)
                                      : hpRatio > 0.25f ? new Color(0.95f, 0.8f, 0.15f)
                                                        : new Color(0.9f, 0.3f, 0.2f);

                    m.bg.color = isSel ? new Color(0.55f, 0.42f, 0.02f, 0.85f)
                                       : new Color(0f, 0f, 0f, 0.62f);
                    float s = (isSel ? 1.15f : 1f) * (isTop ? pulse : 1f);
                    m.root.localScale = Vector3.one * s;
                }
                else
                {
                    if (dir.sqrMagnitude < 1f) dir = Vector2.down;
                    float scale = Mathf.Min(maxX / Mathf.Max(Mathf.Abs(dir.x), 0.0001f),
                                            maxY / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
                    finalScreen = screenCenter + dir * scale;

                    m.onScreen.SetActive(false);
                    m.offScreen.SetActive(true);

                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    m.arrow.localRotation = Quaternion.Euler(0f, 0f, ang - 90f);
                    m.arrowTxt.color   = threat;
                    m.arrowDist.color  = threat;
                    m.arrowDist.text   = $"{distBase:0}m";

                    // 越急越大（ETA 小 → 大）+ 選中/最急再放大脈動
                    float urg = 1f - Mathf.Clamp01(eta / 30f);
                    float s   = Mathf.Lerp(1f, 1.5f, urg) * (isSel ? 1.2f : 1f) * (isTop ? pulse : 1f);
                    m.root.localScale = Vector3.one * s;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, finalScreen, null, out Vector2 local);
                m.root.anchoredPosition = local;
            }
        }

        private void OnMarkerClicked(Marker m)
        {
            if (m.target == null) return;
            _selected = m;
            if (_artillery == null) _artillery = FindAnyObjectByType<ArtilleryController>();
            Vector3 vel = m.target is Enemy en ? en.Velocity : Vector3.zero;
            _artillery?.AimAtTarget(m.target.transform.position, vel);
        }

        // ── 威脅 ─────────────────────────────────────────────────────
        private static float Eta(DestructibleTarget dt) => dt is Enemy e ? e.Eta : 999f;

        private static Color ThreatColor(float eta) =>
            eta < DangerEta ? ColDanger : eta < WarnEta ? ColWarn : ColSafe;

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
            Color typeCol = TargetColor(dt);
            var marker = new Marker { target = dt };

            var root = new GameObject($"Marker_{dt.name}");
            root.transform.SetParent(_canvasRect, false);
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(190f, 140f);
            marker.root = rootRT;

            var click = root.AddComponent<Image>();
            click.color = new Color(0f, 0f, 0f, 0f);
            click.raycastTarget = true;
            var btn = root.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var captured = marker;
            btn.onClick.AddListener(() => OnMarkerClicked(captured));

            // ── 畫面內群組 ──
            var onScreen = MakeGroup("OnScreen", root.transform, rootRT.sizeDelta);
            marker.onScreen = onScreen;
            marker.bg = AddImage(onScreen.transform, "Bg", new Color(0f, 0f, 0f, 0.62f),
                new Vector2(0f, 8f), new Vector2(160f, 40f), false);
            marker.label = AddText(onScreen.transform, "Label", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 8f), new Vector2(170f, 40f));
            AddImage(onScreen.transform, "Icon", typeCol, new Vector2(0f, 40f), new Vector2(20f, 20f), false);
            var hpBg = AddImage(onScreen.transform, "HpBg", new Color(0f, 0f, 0f, 0.7f),
                new Vector2(0f, -18f), new Vector2(132f, 12f), false);
            marker.hpFillImg = AddImage(hpBg.transform, "HpFill", new Color(0.35f, 0.85f, 0.35f),
                Vector2.zero, Vector2.zero, false);
            marker.hpFillRT = marker.hpFillImg.rectTransform;
            marker.hpFillRT.anchorMin = new Vector2(0f, 0f);
            marker.hpFillRT.anchorMax = new Vector2(1f, 1f);
            marker.hpFillRT.offsetMin = new Vector2(2f, 2f);
            marker.hpFillRT.offsetMax = new Vector2(-2f, -2f);

            // ── 離屏群組（放大箭頭 + 距離）──
            var offScreen = MakeGroup("OffScreen", root.transform, rootRT.sizeDelta);
            marker.offScreen = offScreen;
            marker.arrowTxt = AddText(offScreen.transform, "Arrow", 58, TextAnchor.MiddleCenter,
                new Vector2(0f, 10f), new Vector2(70f, 70f));
            marker.arrowTxt.text  = "▲";
            marker.arrowTxt.color = typeCol;
            marker.arrow = marker.arrowTxt.rectTransform;
            marker.arrowDist = AddText(offScreen.transform, "ArrowDist", 22, TextAnchor.MiddleCenter,
                new Vector2(0f, -32f), new Vector2(120f, 28f));
            offScreen.SetActive(false);

            return marker;
        }

        private GameObject MakeGroup(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return go;
        }

        private static Image AddImage(Transform parent, string name, Color color,
            Vector2 pos, Vector2 size, bool raycast)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        private Text AddText(Transform parent, string name, int size, TextAnchor anchor,
            Vector2 pos, Vector2 sizeD)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.fontStyle = FontStyle.Bold;
            t.color = Color.white; t.alignment = anchor; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeD;
            return t;
        }

        // ── 工具 ─────────────────────────────────────────────────────
        private static Vector3 TargetTop(DestructibleTarget dt)
        {
            float maxY = dt.transform.position.y;
            foreach (var r in dt.GetComponentsInChildren<Renderer>())
                if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
            return new Vector3(dt.transform.position.x, maxY + 2.5f, dt.transform.position.z);
        }

        private static Color TargetColor(DestructibleTarget dt)
        {
            if (dt is Enemy e)
            {
                return e.Type switch
                {
                    EnemyType.Goblin => new Color(0.55f, 0.85f, 0.35f),
                    EnemyType.Orc    => new Color(0.85f, 0.65f, 0.35f),
                    EnemyType.Beetle => new Color(0.65f, 0.65f, 0.72f),
                    EnemyType.Demon  => new Color(1f,    0.35f, 0.30f),
                    _                => Color.white,
                };
            }
            return new Color(0.9f, 0.9f, 0.9f);
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
