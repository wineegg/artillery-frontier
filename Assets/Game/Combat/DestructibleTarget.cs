using System.Collections;
using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public enum ResourceType { Stone, Iron, Sulfur }

    [System.Serializable]
    public struct LootEntry
    {
        public ResourceType type;
        public int          min;
        public int          max;
        [Range(0f, 1f)]
        public float        chance;
    }

    public class DestructibleTarget : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected float maxHP = 100f;
        [SerializeField] protected float armor = 0f;

        [Header("Drops")]
        [SerializeField] private LootEntry[] dropTable = new LootEntry[0];

        protected float    _hp;
        private Renderer[] _renderers;
        private Color[]    _baseColors;

        protected virtual void Awake()
        {
            _hp = maxHP;
            CacheRenderers();
        }

        private IEnumerator Start()
        {
            // Hide for one frame so terrain can be created by EnvironmentBuilder.Start()
            SetVisible(false);
            yield return null;
            SnapToTerrain();
            SetVisible(true);
        }

        // ── 傷害 / 死亡 ──────────────────────────────────────────────
        public virtual void Impact(float rawDamage)
        {
            float eff = Mathf.Max(1f, rawDamage - armor);
            _hp = Mathf.Max(0f, _hp - eff);
            PaintDamage();
            if (_hp <= 0f) Die();
        }

        protected virtual void Die()
        {
            ImpactEffect.Spawn(transform.position + Vector3.up * 0.8f, Vector3.up);
            SpawnLoot();
            Destroy(gameObject);
        }

        // ── 視覺回饋 ─────────────────────────────────────────────────
        private void PaintDamage()
        {
            float ratio = _hp / maxHP;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Color tint = Color.Lerp(new Color(1f, 0.08f, 0.04f), _baseColors[i], ratio);
                _renderers[i].material.color = tint;
            }
        }

        // ── 掉落 ─────────────────────────────────────────────────────
        private void SpawnLoot()
        {
            Vector3 lootOrigin = transform.position + Vector3.up;
            foreach (var e in dropTable)
            {
                if (Random.value <= e.chance)
                    FloatingLoot.Spawn(lootOrigin, e.type, Random.Range(e.min, e.max + 1));
            }
        }

        // ── 地形貼合 ─────────────────────────────────────────────────
        private void SnapToTerrain()
        {
            if (Terrain.activeTerrain == null) return;

            Vector3 pos    = transform.position;
            float groundY  = Terrain.activeTerrain.SampleHeight(pos)
                           + Terrain.activeTerrain.transform.position.y;

            float minY = float.MaxValue;
            foreach (var r in _renderers)
                minY = Mathf.Min(minY, r.bounds.min.y);

            float shift = (minY < float.MaxValue) ? (groundY - minY) : (groundY - pos.y);
            transform.position = new Vector3(pos.x, pos.y + shift, pos.z);
        }

        // ── 工具 ─────────────────────────────────────────────────────
        private void CacheRenderers()
        {
            _renderers  = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _baseColors[i] = _renderers[i].material.color;
        }

        private void SetVisible(bool v)
        {
            foreach (var r in _renderers) r.enabled = v;
        }

        protected void SetDropTable(LootEntry[] table) => dropTable = table;
    }
}
