using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public class ResourceNode : DestructibleTarget
    {
        [SerializeField] private ResourceType nodeType;
        public ResourceType NodeType => nodeType;

        protected override void Awake()
        {
            AutoConfigure();
            base.Awake();
        }

        private void AutoConfigure()
        {
            switch (nodeType)
            {
                case ResourceType.Stone:
                    maxHP = 80f; armor = 0f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Stone, min = 3, max = 8, chance = 1f }
                    });
                    break;

                case ResourceType.Iron:
                    maxHP = 150f; armor = 10f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Iron,  min = 2, max = 5, chance = 1f  },
                        new LootEntry { type = ResourceType.Stone, min = 1, max = 3, chance = 0.5f }
                    });
                    break;

                case ResourceType.Sulfur:
                    maxHP = 60f; armor = 0f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Sulfur, min = 2, max = 4, chance = 1f }
                    });
                    break;
            }
        }
    }
}
