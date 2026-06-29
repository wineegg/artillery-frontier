using System.Collections.Generic;
using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        private readonly Dictionary<ResourceType, int> _res = new();

        private void Awake() => Instance = this;

        public void Add(ResourceType type, int amount)
        {
            _res.TryGetValue(type, out int cur);
            _res[type] = cur + amount;
            ArtilleryFrontier.UI.ArtilleryHUD.RefreshInventory(_res);
        }

        public int Get(ResourceType type) => _res.TryGetValue(type, out int v) ? v : 0;
    }
}
