using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    // 自動掛在 ArtilleryBase 上，無需手動連接引用
    public class AngleVisualizer : MonoBehaviour
    {
        [Header("Ground Arc")]
        [SerializeField] private float arcRadius = 4.8f;
        [SerializeField] private Color arcColor     = new Color(0.35f, 0.85f, 1f, 0.35f);
        [SerializeField] private Color tickColor    = new Color(1f,    1f,    1f, 0.55f);
        [SerializeField] private Color pointerColor = new Color(0.2f,  1f,    0.4f, 1f);

        [Header("Angle Display")]
        [SerializeField] private float displayDistance = 0.5f; // 砲口前方距離

        private ArtilleryController _ctrl;
        private Transform _muzzle;
        private LineRenderer _pointer;
        private readonly List<Transform> _billboardLabels = new List<Transform>();
        private Camera _mainCam;

        // World Space UI
        private RectTransform _canvasRect;
        private Text _yawText;
        private Text _pitchText;

        private Vector3 _arcOrigin; // 弧形圓心（世界座標，地面高度）

        private void Start()
        {
            _ctrl    = GetComponent<ArtilleryController>()
                    ?? GetComponentInParent<ArtilleryController>();
            _mainCam = Camera.main;

            // 找 Muzzle
            var muzzleGO = transform.root.Find("ArtilleryBase/BarrelPivot/Muzzle")
                        ?? FindDeepChild(transform.root, "Muzzle");
            _muzzle = muzzleGO;

            // 弧心在地面
            _arcOrigin = new Vector3(transform.position.x, 0.02f, transform.position.z);

            BuildRangeArc();
            BuildTickMarks();
            BuildDirectionPointer();
            BuildAngleDisplay();
        }

        private void LateUpdate()
        {
            if (_ctrl == null) return;
            UpdatePointer();
            UpdateAngleDisplay();
            BillboardLabels();
        }

        // ── 靜態：±90° 弧形範圍線 ─────────────────────────────────────
        private void BuildRangeArc()
        {
            var go = new GameObject("RangeArc");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            const int segs = 72;
            lr.positionCount = segs + 1;
            lr.startWidth = 0.05f;
            lr.endWidth   = 0.05f;
            lr.useWorldSpace = true;
            lr.sortingOrder = 5;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.material = MakeSpriteMat(arcColor);
            lr.startColor = arcColor;
            lr.endColor   = arcColor;

            for (int i = 0; i <= segs; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, i / (float)segs) * Mathf.Deg2Rad;
                lr.SetPosition(i, _arcOrigin + new Vector3(
                    Mathf.Sin(a) * arcRadius, 0f, Mathf.Cos(a) * arcRadius));
            }
        }

        // ── 靜態：刻度線 + 角度標籤 ───────────────────────────────────
        private void BuildTickMarks()
        {
            int[] degs = { -90, -60, -30, 0, 30, 60, 90 };

            foreach (int deg in degs)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 dir   = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 outer = _arcOrigin + dir * (arcRadius + 0.3f);
                Vector3 inner = _arcOrigin + dir * (arcRadius - 0.45f);

                // 刻度短線
                var tickGO = new GameObject($"Tick_{deg}");
                tickGO.transform.SetParent(transform);
                var lr = tickGO.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                bool isMajor = (deg % 90 == 0);
                lr.startWidth = isMajor ? 0.07f : 0.04f;
                lr.endWidth   = lr.startWidth;
                lr.useWorldSpace = true;
                lr.sortingOrder = 6;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.material = MakeSpriteMat(isMajor ? Color.white : tickColor);
                lr.startColor = isMajor ? Color.white : tickColor;
                lr.endColor   = lr.startColor;
                lr.SetPosition(0, inner + Vector3.up * 0.01f);
                lr.SetPosition(1, outer + Vector3.up * 0.01f);

                // 角度標籤（TextMesh，LateUpdate 中 billboard）
                var labelGO = new GameObject($"Label_{deg}");
                labelGO.transform.SetParent(transform);
                labelGO.transform.position = outer + dir * 0.55f + Vector3.up * 0.08f;

                var tm = labelGO.AddComponent<TextMesh>();
                tm.text      = $"{deg}°";
                tm.fontSize  = 10;
                tm.characterSize = 0.28f;
                tm.color     = isMajor
                    ? new Color(1f, 1f, 1f, 0.9f)
                    : new Color(0.85f, 0.85f, 0.85f, 0.6f);
                tm.anchor    = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;

                var mr = labelGO.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                _billboardLabels.Add(labelGO.transform);
            }
        }

        // ── 動態：當前 Yaw 方向指針 ────────────────────────────────────
        private void BuildDirectionPointer()
        {
            var go = new GameObject("DirectionPointer");
            go.transform.SetParent(transform);

            _pointer = go.AddComponent<LineRenderer>();
            _pointer.positionCount = 2;
            _pointer.startWidth = 0.10f;
            _pointer.endWidth   = 0.02f;
            _pointer.useWorldSpace = true;
            _pointer.sortingOrder = 8;
            _pointer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pointer.receiveShadows = false;

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(pointerColor, 0f), new GradientColorKey(new Color(pointerColor.r, pointerColor.g, pointerColor.b, 0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            _pointer.colorGradient = grad;
            _pointer.material = MakeSpriteMat(pointerColor);
        }

        private void UpdatePointer()
        {
            float yaw = _ctrl.GetCurrentYaw() * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            Vector3 start = _arcOrigin + Vector3.up * 0.04f;
            Vector3 end   = _arcOrigin + dir * (arcRadius + 0.9f) + Vector3.up * 0.04f;
            _pointer.SetPosition(0, start);
            _pointer.SetPosition(1, end);
        }

        // ── World Space UI：Yaw / Pitch 即時數值 ───────────────────────
        private void BuildAngleDisplay()
        {
            var canvasGO = new GameObject("AngleDisplay");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            _canvasRect = canvasGO.GetComponent<RectTransform>();
            _canvasRect.sizeDelta = new Vector2(220f, 70f);
            canvasGO.transform.localScale = Vector3.one * 0.005f;

            // 半透明底色
            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var img = bgGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.60f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // 邊框線條感（黃色細框）
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(canvasGO.transform, false);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.85f, 0f, 0.5f);
            var borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2f, -2f);
            borderRect.offsetMax = new Vector2(2f, 2f);
            borderImg.transform.SetSiblingIndex(0);

            _yawText   = MakeUIText(canvasGO.transform, new Vector2(0f, 16f),  "YAW:   0.0°");
            _pitchText = MakeUIText(canvasGO.transform, new Vector2(0f, -16f), "PITCH: 0.0°");
        }

        private void UpdateAngleDisplay()
        {
            if (_yawText == null) return;

            _yawText.text   = $"YAW:   {_ctrl.GetCurrentYaw():+0.0;-0.0;  0.0}°";
            _pitchText.text = $"PITCH: {_ctrl.GetCurrentPitch():0.0}°";

            // 跟隨砲口位置（正前方偏上）
            if (_muzzle != null)
            {
                _canvasRect.transform.position = _muzzle.position
                    + Vector3.up * 0.6f
                    + _muzzle.forward * displayDistance;
            }

            // Billboard：永遠面向攝影機
            if (_mainCam != null)
            {
                Vector3 toCamera = _canvasRect.transform.position - _mainCam.transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                    _canvasRect.transform.rotation = Quaternion.LookRotation(toCamera);
            }
        }

        private void BillboardLabels()
        {
            if (_mainCam == null) return;
            foreach (var t in _billboardLabels)
            {
                Vector3 toCamera = t.position - _mainCam.transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                    t.rotation = Quaternion.LookRotation(toCamera);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────
        private static Text MakeUIText(Transform parent, Vector2 anchoredPos, string content)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text      = content;
            text.fontSize  = 28;
            text.fontStyle = FontStyle.Bold;
            text.color     = new Color(1f, 0.95f, 0.6f, 1f);
            text.alignment = TextAnchor.MiddleLeft;

            // Unity 6 built-in font
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.font = f;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(200f, 30f);

            return text;
        }

        private static Material MakeSpriteMat(Color color)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            mat.renderQueue = 3500;
            return mat;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
