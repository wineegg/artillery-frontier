using UnityEngine;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Combat
{
    /// <summary>
    /// 掉落視覺點綴。資源在生成當下（= 目標摧毀）立即結算，
    /// 因此不論目標多遠都保證「命中 → +N → 資源欄更新」。
    /// 這裡的寶石只負責在命中點浮起淡出，提供空間回饋。
    /// </summary>
    public class FloatingLoot : MonoBehaviour
    {
        private ResourceType _type;
        private int          _amount;
        private float        _age;
        private Material      _mat;
        private Color         _baseColor;

        private const float RiseSpeed = 3f;
        private const float Lifetime  = 1.4f;

        public static void Spawn(Vector3 pos, ResourceType type, int amount)
        {
            var go = new GameObject($"Loot_{type}");
            go.transform.position = pos;

            var fl = go.AddComponent<FloatingLoot>();
            fl._type   = type;
            fl._amount = amount;
            fl.BuildVisual();

            // 命中即獲得：立即結算資源（觸發 +N 彈窗與資源欄更新）
            GameEvents.RaiseLootCollected(type, amount);
        }

        private void BuildVisual()
        {
            var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.transform.SetParent(transform);
            gem.transform.localPosition = Vector3.zero;
            gem.transform.localScale    = Vector3.one * 2.5f;  // 目標遠(250m+)，放大才看得見
            Destroy(gem.GetComponent<Collider>());

            _baseColor = TypeColor(_type);
            _mat = new Material(Shader.Find("Sprites/Default")) { color = _baseColor };
            gem.GetComponent<Renderer>().material = _mat;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            transform.Rotate(0f, 180f * Time.deltaTime, 0f);

            if (_mat != null)
            {
                float a = Mathf.Clamp01(1f - _age / Lifetime);
                _mat.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
            }

            if (_age >= Lifetime) Destroy(gameObject);
        }

        private static Color TypeColor(ResourceType t) => t switch
        {
            ResourceType.Stone  => new Color(0.60f, 0.60f, 0.65f),
            ResourceType.Iron   => new Color(0.65f, 0.30f, 0.12f),
            ResourceType.Sulfur => new Color(0.95f, 0.88f, 0.08f),
            _                   => Color.white,
        };
    }
}
