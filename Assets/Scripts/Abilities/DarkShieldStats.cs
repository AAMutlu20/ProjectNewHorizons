using System;

namespace Abilities
{
    /// <summary>
    /// Per-rarity stats for Dark Shield, matching the design doc exactly.
    /// Layer Regen and Shield Regen are identical across all four tiers in
    /// the doc — only Layers scales — but kept per-tier here rather than as
    /// separate constants, in case future balance passes want to vary them.
    ///
    /// Cooldown is unused by Dark Shield (it isn't a cast-on-cooldown ability)
    /// but still implemented to satisfy IAbilityRarityStats, returning 0 so
    /// nothing that happens to read it misbehaves.
    /// </summary>
    [Serializable]
    public struct DarkShieldStats : IAbilityRarityStats
    {
        public int Layers;
        public float LayerRegenSeconds;
        public float FullShieldRegenSeconds;

        float IAbilityRarityStats.Cooldown => 0f;
    }
}
