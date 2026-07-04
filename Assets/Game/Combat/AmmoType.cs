using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public enum AmmoType { Normal, AP, Explosive, Incendiary, Freeze }

    /// <summary>單發砲彈的規格。</summary>
    public struct AmmoSpec
    {
        public string  name;         // HUD 顯示
        public float   damage;       // 直擊 / AoE 傷害
        public bool    ignoreArmor;  // 穿透彈：無視護甲
        public float   aoeRadius;    // >0 = 範圍爆炸（不做單體直擊）
        public float   burnDps;      // >0 = 點燃每秒傷害（無視護甲）
        public float   burnTime;
        public float   slowFactor;   // 0<f<1 = 減速到該倍速
        public float   slowTime;
        public Color   color;
    }

    /// <summary>各彈種規格（單一調校來源）。對應 d.png 的五種砲彈與敵人克制。</summary>
    public static class AmmoConfig
    {
        public static readonly AmmoType[] Order =
            { AmmoType.Normal, AmmoType.AP, AmmoType.Explosive, AmmoType.Incendiary, AmmoType.Freeze };

        public static AmmoSpec Get(AmmoType t) => t switch
        {
            AmmoType.AP => new AmmoSpec {
                name = "AP", damage = 55f, ignoreArmor = true,
                color = new Color(0.35f, 0.60f, 1f) },

            AmmoType.Explosive => new AmmoSpec {
                name = "FRAG", damage = 30f, aoeRadius = 14f,
                color = new Color(1f, 0.45f, 0.15f) },

            AmmoType.Incendiary => new AmmoSpec {
                name = "FIRE", damage = 20f, burnDps = 12f, burnTime = 4f,
                color = new Color(1f, 0.35f, 0.10f) },

            AmmoType.Freeze => new AmmoSpec {
                name = "ICE", damage = 15f, slowFactor = 0.4f, slowTime = 3f,
                color = new Color(0.45f, 0.85f, 1f) },

            _ => new AmmoSpec {   // Normal
                name = "HE", damage = 50f,
                color = new Color(1f, 0.75f, 0.25f) },
        };
    }
}
