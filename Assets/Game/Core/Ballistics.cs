using UnityEngine;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 共用確定性彈道。砲彈（Projectile）與落點預測（LandingPreview）都呼叫這裡，
    /// 使用相同積分步長與相同地面查詢 → 保證「預測落點 = 實際落點」。
    /// </summary>
    public static class Ballistics
    {
        /// 重力（負值），套用設定倍率。
        public static float Gravity => Physics.gravity.y * GameConfig.GravityMultiplier;

        /// 地形表面高度（世界座標）；無地形時視為 y=0。
        public static float GroundY(Vector3 pos)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(pos) + t.transform.position.y : 0f;
        }

        /// 單步半隱式歐拉積分（砲彈與預測共用）。
        public static void Step(ref Vector3 pos, ref Vector3 vel, float dt)
        {
            vel.y += Gravity * dt;
            pos   += vel * dt;
        }

        public struct Result
        {
            public Vector3 point;     // 落點
            public float   time;      // 飛行時間
            public bool    grounded;  // 是否真的落地（false = 超時未落地）
        }

        /// 解算命中目標所需的仰角（度，低伸彈道）。回傳 NaN 表示射程不足。
        public static float SolveElevation(Vector3 muzzle, Vector3 target, float speed)
        {
            float dx = target.x - muzzle.x;
            float dz = target.z - muzzle.z;
            float x  = Mathf.Sqrt(dx * dx + dz * dz);   // 水平距離
            float y  = target.y - muzzle.y;             // 高度差
            if (x < 0.01f || speed < 0.01f) return float.NaN;

            float g = -Gravity;                          // 重力大小（正）
            float a = g * x * x / (2f * speed * speed);
            float disc = x * x - 4f * a * (y + a);
            if (disc < 0f) return float.NaN;             // 射程不足

            float t = (x - Mathf.Sqrt(disc)) / (2f * a); // 較小根 = 低伸彈道
            return Mathf.Atan(t) * Mathf.Rad2Deg;
        }

        /// 朝向目標的水平方位角（度，0 = +Z）。
        public static float SolveYaw(Vector3 muzzle, Vector3 target)
        {
            float dx = target.x - muzzle.x;
            float dz = target.z - muzzle.z;
            return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        }

        /// 模擬彈道直到落地或超時。dt 建議傳 Time.fixedDeltaTime 以對齊砲彈實際運動。
        public static Result Predict(Vector3 origin, Vector3 velocity, float dt, float maxTime)
        {
            Vector3 pos = origin, vel = velocity;
            float   t   = 0f;
            int     max = Mathf.CeilToInt(maxTime / dt);

            for (int i = 0; i < max; i++)
            {
                Step(ref pos, ref vel, dt);
                t += dt;

                float gy = GroundY(pos);
                if (pos.y <= gy)
                {
                    pos.y = gy;
                    return new Result { point = pos, time = t, grounded = true };
                }
            }
            return new Result { point = pos, time = t, grounded = false };
        }
    }
}
