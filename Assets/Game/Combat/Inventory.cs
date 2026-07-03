using System.Collections.Generic;
using UnityEngine;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Combat
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        private readonly Dictionary<ResourceType, int> _res = new();

        private void Awake() => Instance = this;

        private void OnEnable()  => GameEvents.LootCollected += OnLootCollected;
        private void OnDisable() => GameEvents.LootCollected -= OnLootCollected;

        private void OnLootCollected(ResourceType type, int amount) => Add(type, amount);

        public void Add(ResourceType type, int amount)
        {
            _res.TryGetValue(type, out int cur);
            _res[type] = cur + amount;
            GameEvents.RaiseResourceChanged(_res);
        }

        public int Get(ResourceType type) => _res.TryGetValue(type, out int v) ? v : 0;
    }
}
