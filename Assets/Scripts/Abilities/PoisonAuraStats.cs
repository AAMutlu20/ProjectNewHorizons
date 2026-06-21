using System;

namespace Abilities
{
    /// <summary>
    /// Per-rarity stats for Poison Aura, matching the design doc exactly.
    /// SlowFraction is 0 at Base/Rare/Epic — only Legendary applies a slow,
    /// not a separate on/off flag.
    /// </summary>
    [Serializable]
    public struct PoisonAuraStats : IAbilityRarityStats
    {
        public float Damage;
        public float DamageFrequencySeconds;
        public float Range;
        public float SlowFraction;

        // Poison Aura has no cooldown — it's a persistent aura, not a
        // cast-on-cooldown ability. Returns 0 to satisfy the interface.
        float IAbilityRarityStats.Cooldown => 0f;
    }
}
