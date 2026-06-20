using System;

namespace Abilities
{
    /// <summary>Per-rarity stats for Shockwave, matching the design doc exactly.</summary>
    [Serializable]
    public struct ShockwaveStats : IAbilityRarityStats
    {
        public float Damage;
        public float Radius;
        public float Cooldown;
        public float StunDuration;

        float IAbilityRarityStats.Cooldown => Cooldown;
    }
}
