using UnityEngine;
using UnityEngine.UI;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.UI
{
    public class RewardPopup : MonoBehaviour
    {
        private Text    _text;
        private float   _life;
        private Color   _baseColor;

        private const float Duration  = 2.2f;
        private const float FloatUp   = 55f;   // px

        public static void Show(ResourceType type, int amount)
        {
            string label = type == ResourceType.Stone  ? "Stone"
                         : type == ResourceType.Iron   ? "Iron"
                                                       : "Sulfur";
            Color c = type == ResourceType.Stone  ? new Color(0.85f, 0.85f, 0.92f)
                    : type == ResourceType.Iron    ? new Color(0.85f, 0.55f, 0.22f)
                                                   : new Color(1.0f,  0.92f, 0.10f);
            Spawn($"+{amount} {label}", c);
        }

        private static void Spawn(string msg, Color color)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var root = new GameObject("RewardPopup");
            var cv   = root.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 500;
            root.AddComponent<CanvasScaler>();

            var textGO = new GameObject("Msg");
            textGO.transform.SetParent(root.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.text      = msg;
            txt.fontSize  = 52;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = font;

            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.42f);
            rt.anchorMax        = new Vector2(0.5f, 0.42f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(380f, 70f);
            rt.anchoredPosition = Vector2.zero;

            var popup = root.AddComponent<RewardPopup>();
            popup._text      = txt;
            popup._baseColor = color;
        }

        private void Update()
        {
            _life += Time.deltaTime;
            float frac = _life / Duration;

            var rt = _text.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, frac * FloatUp);

            float alpha = frac < 0.35f ? 1f : Mathf.Clamp01(1f - (frac - 0.35f) / 0.65f);
            _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

            if (_life >= Duration)
                Destroy(gameObject);
        }
    }
}
