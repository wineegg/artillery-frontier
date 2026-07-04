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

        /// 敵人生成（供標記系統動態加標記）。
        public static event Action<DestructibleTarget> EnemySpawned;

        /// 基地 HP 變動 (目前, 上限)。
        public static event Action<int, int> BaseChanged;

        /// 波次變動 (目前波, 總波)。
        public static event Action<int, int> WaveChanged;

        /// 基地陣亡。
        public static event Action GameOver;

        /// 全波次清除，勝利。
        public static event Action Victory;

        public static void RaiseLootCollected(ResourceType type, int amount)
            => LootCollected?.Invoke(type, amount);

        public static void RaiseResourceChanged(IReadOnlyDictionary<ResourceType, int> res)
            => ResourceChanged?.Invoke(res);

        public static void RaiseTargetDestroyed(DestructibleTarget target)
            => TargetDestroyed?.Invoke(target);

        public static void RaiseAreaCleared()
            => AreaCleared?.Invoke();

        public static void RaiseEnemySpawned(DestructibleTarget enemy)
            => EnemySpawned?.Invoke(enemy);

        public static void RaiseBaseChanged(int hp, int max)
            => BaseChanged?.Invoke(hp, max);

        public static void RaiseWaveChanged(int wave, int total)
            => WaveChanged?.Invoke(wave, total);

        public static void RaiseGameOver()
            => GameOver?.Invoke();

        public static void RaiseVictory()
            => Victory?.Invoke();
    }
}
