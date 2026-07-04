using UnityEngine;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Combat
{
    public enum EnemyType { Goblin, Orc, Beetle, Demon }

    /// <summary>
    /// 會朝基地（原點）前進的可破壞目標。被砲彈打死 → 掉落；攻到基地 → 扣基地 HP。
    /// 用 DestructibleTarget 的 HP/護甲/掉落，額外加移動與到達判定。
    /// </summary>
    public class Enemy : DestructibleTarget
    {
        private EnemyType _type;
        private float     _speed;
        private int       _baseDamage;
        private float     _halfHeight;

        // 狀態效果
        private float _burnDps, _burnTimer;
        private float _slowFactor = 1f, _slowTimer;

        public void ApplyBurn(float dps, float time)
        {
            _burnDps   = Mathf.Max(_burnDps, dps);
            _burnTimer = Mathf.Max(_burnTimer, time);
        }

        public void ApplySlow(float factor, float time)
        {
            _slowFactor = Mathf.Min(_slowFactor, factor);
            _slowTimer  = Mathf.Max(_slowTimer, time);
        }

        public Vector3   Velocity { get; private set; }
        public EnemyType Type     => _type;

        /// 預估抵達基地時間（秒）= 距基地 / 速度。越小越危急（已含速度差異）。
        public float Eta { get; private set; } = 999f;

        private static readonly Vector3 BasePos = Vector3.zero;
        private const float ReachRadius = 14f;

        // ── 工廠：先 inactive 設定型別，再啟用（讓 Awake 讀得到 _type）──
        public static Enemy Spawn(EnemyType type, Vector3 pos)
        {
            var go = new GameObject($"Enemy_{type}");
            go.SetActive(false);
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            var e = go.AddComponent<Enemy>();
            e._type = type;
            e.BuildVisual();

            go.SetActive(true);
            GameEvents.RaiseEnemySpawned(e);
            return e;
        }

        protected override void Awake()
        {
            AutoConfigure();
            base.Awake();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 燃燒（無視護甲的持續傷害）
            if (_burnTimer > 0f)
            {
                _burnTimer -= dt;
                Impact(_burnDps * dt, ignoreArmor: true);
                if (GetHP() <= 0f) return;      // 已死（Die 已呼叫 Destroy）
            }

            // 減速
            float speed = _speed;
            if (_slowTimer > 0f) { _slowTimer -= dt; speed *= _slowFactor; }
            else _slowFactor = 1f;

            Vector3 pos    = transform.position;
            Vector3 toBase = BasePos - pos; toBase.y = 0f;
            float   distXZ = toBase.magnitude;

            Eta = speed > 0.01f ? distXZ / speed : 999f;

            if (distXZ <= ReachRadius)
            {
                WaveManager.Instance?.OnEnemyReachedBase(_baseDamage);
                Destroy(gameObject);           // 到達不掉落
                return;
            }

            Vector3 dir  = toBase / distXZ;
            Vector3 next = pos + dir * (speed * dt);
            next.y = Ballistics.GroundY(next) + _halfHeight;

            Velocity = (next - pos) / Mathf.Max(dt, 1e-4f);
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        protected override void Die()
        {
            WaveManager.Instance?.OnEnemyKilled();
            base.Die();                        // 掉落 + TargetDestroyed + Destroy
        }

        // ── 型別數值（配合砲彈傷害 50；護甲不過高避免變成磨血）──
        private void AutoConfigure()
        {
            switch (_type)
            {
                case EnemyType.Goblin:
                    maxHP = 40f;  armor = 0f;  _speed = 6.5f; _baseDamage = 5;
                    SetDropTable(new[] {
                        new LootEntry { type = ResourceType.Stone, min = 1, max = 3, chance = 1f } });
                    break;
                case EnemyType.Orc:
                    maxHP = 100f; armor = 12f; _speed = 4.5f; _baseDamage = 12;
                    SetDropTable(new[] {
                        new LootEntry { type = ResourceType.Iron,  min = 1, max = 3, chance = 1f },
                        new LootEntry { type = ResourceType.Stone, min = 1, max = 2, chance = 0.6f } });
                    break;
                case EnemyType.Beetle:
                    maxHP = 160f; armor = 25f; _speed = 3f;   _baseDamage = 18;
                    SetDropTable(new[] {
                        new LootEntry { type = ResourceType.Stone, min = 3, max = 6, chance = 1f },
                        new LootEntry { type = ResourceType.Iron,  min = 1, max = 2, chance = 0.7f } });
                    break;
                case EnemyType.Demon:
                    maxHP = 240f; armor = 30f; _speed = 2.2f; _baseDamage = 35;
                    SetDropTable(new[] {
                        new LootEntry { type = ResourceType.Stone,  min = 5, max = 10, chance = 1f },
                        new LootEntry { type = ResourceType.Iron,   min = 3, max = 6,  chance = 1f },
                        new LootEntry { type = ResourceType.Sulfur, min = 2, max = 4,  chance = 0.8f } });
                    break;
            }
        }

        private void BuildVisual()
        {
            (float r, float h, Color c) = _type switch
            {
                EnemyType.Goblin => (1.2f, 2.4f, new Color(0.35f, 0.55f, 0.25f)),
                EnemyType.Orc    => (1.8f, 3.6f, new Color(0.45f, 0.40f, 0.30f)),
                EnemyType.Beetle => (3.0f, 3.2f, new Color(0.30f, 0.30f, 0.33f)),
                EnemyType.Demon  => (4.0f, 7.0f, new Color(0.45f, 0.18f, 0.15f)),
                _                => (1.5f, 3f,   Color.gray),
            };
            _halfHeight = h * 0.5f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(r, h * 0.5f, r);  // Capsule 預設高 2 → h*0.5 得 h
            body.GetComponent<Renderer>().material =
                new Material(Shader.Find("Sprites/Default")) { color = c };
            // 保留 Collider 供砲彈 SphereCast 命中
        }
    }
}
