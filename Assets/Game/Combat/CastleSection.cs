using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public enum StructureType { Wall, Tower, Keep }

    public class CastleSection : DestructibleTarget
    {
        [SerializeField] private StructureType structureType;
        public StructureType SectionType => structureType;

        protected override void Awake()
        {
            AutoConfigure();
            base.Awake();
        }

        private void AutoConfigure()
        {
            switch (structureType)
            {
                case StructureType.Wall:
                    maxHP = 200f; armor = 15f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Stone, min = 5, max = 12, chance = 1f }
                    });
                    break;

                case StructureType.Tower:
                    maxHP = 280f; armor = 20f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Stone, min = 8,  max = 15, chance = 1f  },
                        new LootEntry { type = ResourceType.Iron,  min = 2,  max = 5,  chance = 0.7f }
                    });
                    break;

                case StructureType.Keep:
                    maxHP = 500f; armor = 30f;
                    SetDropTable(new[]
                    {
                        new LootEntry { type = ResourceType.Stone,  min = 15, max = 25, chance = 1f  },
                        new LootEntry { type = ResourceType.Iron,   min = 5,  max = 10, chance = 1f  },
                        new LootEntry { type = ResourceType.Sulfur, min = 3,  max = 6,  chance = 0.8f }
                    });
                    break;
            }
        }
    }
}
