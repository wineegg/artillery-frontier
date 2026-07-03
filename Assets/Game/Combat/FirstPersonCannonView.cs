using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    /// <summary>
    /// 過肩第三人稱視角下，整座砲台皆可見。
    /// 保留此元件僅為確保任何先前第一人稱設定殘留的隱藏 Renderer 全數恢復顯示。
    /// </summary>
    public class FirstPersonCannonView : MonoBehaviour
    {
        private void Start()
        {
            var artBase = GameObject.Find("ArtilleryBase");
            if (artBase == null) return;

            foreach (var r in artBase.GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = true;
        }
    }
}
