using UnityEngine;
using UnityEngine.EventSystems;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.UI
{
    /// <summary>
    /// 附在 HUD 刻度條上的透明互動層。
    /// IsHorizontal=true  → Bearing Strip（左右改變 Yaw）
    /// IsHorizontal=false → Elevation Gauge（上下改變 Pitch）
    /// </summary>
    public class HUDGaugeInput : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [HideInInspector] public ArtilleryController Ctrl;
        [HideInInspector] public bool IsHorizontal;

        private RectTransform _rt;
        private bool _active;

        private void Awake() => _rt = GetComponent<RectTransform>();

        public void OnPointerDown(PointerEventData data) { _active = true;  Apply(data); }
        public void OnDrag(PointerEventData data)        { if (_active) Apply(data); }
        public void OnPointerUp(PointerEventData data)   { _active = false; }

        private void Apply(PointerEventData data)
        {
            if (Ctrl == null || _rt == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rt, data.position, data.pressEventCamera, out Vector2 local))
                return;

            Rect r  = _rt.rect;
            float nx = Mathf.Clamp01((local.x - r.xMin) / r.width);
            float ny = Mathf.Clamp01((local.y - r.yMin) / r.height);

            if (IsHorizontal)
                Ctrl.SetTargetYaw(Mathf.Lerp(-90f, 90f, nx));
            else
                Ctrl.SetTargetPitch(Mathf.Lerp(0f, 80f, ny));
        }
    }
}
