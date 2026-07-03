namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 全遊戲單一調校來源。所有彈道 / 傷害 / 相機參數集中於此，
    /// 取代散落在 VerticalSliceSetup / ProjectileLauncher / LandingPreview 的魔術數字。
    /// </summary>
    public static class GameConfig
    {
        // ── 彈道 ──────────────────────────────────────────────────────
        // 48 m/s → 最大射程 ~235m；目標 125-156m 落在舒適的 20-70° 仰角帶，
        // pitch 微調不會過於敏感（110m/s 時近距目標只需 ~3° 太難調）。
        public const float MuzzleSpeed        = 48f;
        public const float GravityMultiplier  = 1f;    // ×Physics.gravity
        public const float ProjectileRadius   = 0.30f; // SphereCast 半徑
        public const float ProjectileLifetime = 12f;   // 秒，超時自毀
        public const float SimMaxTime         = 30f;   // 落點模擬上限（秒）

        // ── 戰鬥 ──────────────────────────────────────────────────────
        public const float ShellDamage  = 50f;
        public const float FireCooldown = 0.6f;

        // ── 相機 ──────────────────────────────────────────────────────
        public const float AimFOV       = 55f;
        public const float ObserveFOV   = 25f;
        public const float FireTrauma   = 0.45f;
        public const float ImpactTrauma = 0.55f;

        // 輕度過肩第三人稱瞄準機位（相對 ArtilleryBase，僅隨 Yaw 轉，脫離砲管俯仰）
        public const float AimCamSide = 0.7f;    // 側移（右）
        public const float AimCamUp   = 2.5f;    // 抬高
        public const float AimCamBack = -4.0f;   // 後退
        public const float AimCamTilt = 12f;     // 固定下俯角（度）
    }
}
