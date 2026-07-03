using System;
using System.Collections.Generic;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 輕量事件匯流排。取代 Inventory/HUD/Castle/Loot 之間的跨層硬呼叫與單例互戳。
    /// 訂閱者請於 OnEnable 訂閱、OnDisable 取消訂閱（避免 Editor 關閉 Domain Reload 時殘留）。
    /// </summary>
    public static class GameEvents
    {
        /// 撿到資源（砲彈掉落物飛入相機時）。
        public static event Action<ResourceType, int> LootCollected;

        /// 庫存數量變動後（供 HUD 更新數字）。
        public static event Action<IReadOnlyDictionary<ResourceType, int>> ResourceChanged;

        /// 任何可破壞目標被摧毀（CastleManager 依型別過濾）。
        public static event Action<DestructibleTarget> TargetDestroyed;

        /// 城堡全數清除。
        public static event Action AreaCleared;

        public static void RaiseLootCollected(ResourceType type, int amount)
            => LootCollected?.Invoke(type, amount);

        public static void RaiseResourceChanged(IReadOnlyDictionary<ResourceType, int> res)
            => ResourceChanged?.Invoke(res);

        public static void RaiseTargetDestroyed(DestructibleTarget target)
            => TargetDestroyed?.Invoke(target);

        public static void RaiseAreaCleared()
            => AreaCleared?.Invoke();
    }
}
