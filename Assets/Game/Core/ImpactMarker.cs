using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 命中標記：紅色圓圈（3 秒漸隱）+ WorldSpace 文字顯示距離 / ResourceNode HP。
    /// </summary>
    public class ImpactMarker : MonoBehaviour
    {
        // ── 公開產生介面 ──────────────────────────────────────────────
        public static void Spawn(Vector3 point, float distance, DestructibleTarget target)
        {
            var go = new GameObject("ImpactMarker");
            var m  = go.AddComponent<ImpactMarker>();
            m._point    = point;
            m._distance = distance;
            m._target   = target;
        }

        // ── 資料 ─────────────────────────────────────────────────────
        private Vector3            _point;
        private float              _distance;
        private DestructibleTarget _target;

        private const float Duration = 3f;

        private void Start() { StartCoroutine(Run()); }

        // ── 主流程 ───────────────────────────────────────────────────
        private IEnumerator Run()
        {
            // 讀取 HP（Impact 已執行，此時為衝擊後的 HP）
            var node = _target as ResourceNode;
            float hp    = node?.GetHP()    ?? -1f;
            float maxHP = node?.GetMaxHP() ?? -1f;

            var ring  = BuildRing(_point);
            var label = BuildLabel(_point, _distance, hp, maxHP);

            var ringLr   = ring.GetComponent<LineRenderer>();
            var bgImg    = label.GetComponentInChildren<Image>();
            var allTexts = label.GetComponentsInChildren<Text>();

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - elapsed / Duration;

                // 圓圈漸隱
                if (ringLr != null)
                {
                    var c = ringLr.startColor;
                    c.a = alpha;
                    ringLr.startColor = c;
                    ringLr.endColor   = c;
                }

                // 文字漸隱
                if (bgImg != null)
                    bgImg.color = new Color(0f, 0f, 0f, alpha * 0.55f);
                foreach (var t in allTexts)
                    t.color = new Color(t.color.r, t.color.g, t.color.b, alpha);

                // 標籤面朝相機
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 dir = cam.transform.position - label.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        label.transform.rotation = Quaternion.LookRotation(-dir);
                }

                yield return null;
            }

            Destroy(ring);
            Destroy(label);
            Destroy(gameObject);
        }

        // ── 紅色 LineRenderer 圓圈 ────────────────────────────────────
        private static GameObject BuildRing(Vector3 center)
        {
            var go = new GameObject("ImpactRing");
            var lr = go.AddComponent<LineRenderer>();
            lr.loop             = true;
            lr.positionCount    = 48;
            lr.startWidth       = 0.55f;
            lr.endWidth         = 0.55f;
            lr.useWorldSpace    = true;

            const float R = 5f;
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(a) * R, 0.25f, Mathf.Sin(a) * R));
            }

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.05f, 0.05f, 1f);
            lr.material = mat;
            return go;
        }

        // ── WorldSpace 距離 + HP 文字 ────────────────────────────────
        private static GameObject BuildLabel(Vector3 pos,
            float dist, float hp, float maxHP)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            bool hasHP = (hp >= 0f);
            float height = hasHP ? 80f : 50f;

            var root = new GameObject("ImpactLabel");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 15;

            var crt = root.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(180f, height);
            root.transform.localScale = Vector3.one * 0.06f;   // world ~10.8m × ~5m；200m 時文字 ~21px
            root.transform.position   = pos + Vector3.up * 5f;

            // 半透明背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(root.transform, false);
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(bg.GetComponent<RectTransform>());

            float topRatio = hasHP ? 0.5f : 0f;

            // 距離文字
            var dGO = new GameObject("Dist");
            dGO.transform.SetParent(root.transform, false);
            var dTxt = dGO.AddComponent<Text>();
            dTxt.text      = $"DIST  {dist:0} m";
            dTxt.font      = font;
            dTxt.fontSize  = 46;
            dTxt.fontStyle = FontStyle.Bold;
            dTxt.color     = new Color(1f, 0.9f, 0.2f);
            dTxt.alignment = TextAnchor.MiddleCenter;
            var dRt = dGO.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, topRatio);
            dRt.anchorMax = Vector2.one;
            dRt.offsetMin = dRt.offsetMax = Vector2.zero;

            // HP 文字（僅 ResourceNode）
            if (hasHP)
            {
                var hGO = new GameObject("HP");
                hGO.transform.SetParent(root.transform, false);
                var hTxt = hGO.AddComponent<Text>();
                hTxt.text      = $"HP  {hp:0} / {maxHP:0}";
                hTxt.font      = font;
                hTxt.fontSize  = 40;
                hTxt.color     = new Color(1f, 0.3f, 0.1f);
                hTxt.alignment = TextAnchor.MiddleCenter;
                var hRt = hGO.GetComponent<RectTransform>();
                hRt.anchorMin = Vector2.zero;
                hRt.anchorMax = new Vector2(1f, topRatio);
                hRt.offsetMin = hRt.offsetMax = Vector2.zero;
            }

            return root;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
