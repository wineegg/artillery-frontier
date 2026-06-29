using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public class FloatingLoot : MonoBehaviour
    {
        private ResourceType _type;
        private int   _amount;
        private float _age;

        private const float FloatDuration = 0.9f;
        private const float AbsorbRange   = 22f;
        private const float AbsorbSpeed   = 15f;
        private const float PickupRadius  = 1.8f;

        public static void Spawn(Vector3 pos, ResourceType type, int amount)
        {
            var go = new GameObject($"Loot_{type}");
            go.transform.position = pos;
            var fl    = go.AddComponent<FloatingLoot>();
            fl._type   = type;
            fl._amount = amount;
            fl.BuildVisual();
        }

        private void BuildVisual()
        {
            var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.transform.SetParent(transform);
            gem.transform.localPosition = Vector3.zero;
            gem.transform.localScale    = Vector3.one * 0.4f;
            Destroy(gem.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Sprites/Default")) { color = TypeColor(_type) };
            gem.GetComponent<Renderer>().material = mat;
        }

        private static Color TypeColor(ResourceType t) => t switch
        {
            ResourceType.Stone  => new Color(0.60f, 0.60f, 0.65f),
            ResourceType.Iron   => new Color(0.65f, 0.30f, 0.12f),
            ResourceType.Sulfur => new Color(0.95f, 0.88f, 0.08f),
            _                   => Color.white,
        };

        private void Update()
        {
            _age += Time.deltaTime;
            transform.Rotate(0f, 150f * Time.deltaTime, 0f);

            // Phase 1: float upward
            if (_age < FloatDuration)
            {
                transform.position += Vector3.up * (3f * Time.deltaTime);
                return;
            }

            // Phase 2: drift toward camera, absorb on contact
            Transform cam = Camera.main?.transform;
            if (cam == null) return;

            float dist = Vector3.Distance(transform.position, cam.position);
            if (dist <= AbsorbRange)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, cam.position, AbsorbSpeed * Time.deltaTime);

                if (dist <= PickupRadius)
                {
                    Inventory.Instance?.Add(_type, _amount);
                    Destroy(gameObject);
                }
            }
            else
            {
                // gentle bob while out of range
                transform.position += new Vector3(0f, Mathf.Sin(_age * 2.2f) * 0.015f, 0f);
            }
        }
    }
}
