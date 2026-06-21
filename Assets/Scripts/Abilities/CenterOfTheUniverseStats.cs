using System;

namespace Abilities
{
    /// <summary>
    /// Per-tier stats for Center of the Universe. Deliberately NOT
    /// AbilityDefinitionSo&lt;TStats&gt;-shaped — that base class assumes all
    /// 4 rarities exist, but this ability is Epic/Legendary only per the
    /// design doc (no Base/Rare entries at all, not even disabled ones).
    /// Faking Common/Rare placeholder values just to fit the generic base
    /// would misrepresent data that genuinely doesn't exist for this ability.
    ///
    /// No Cooldown field — this isn't a cast-on-cooldown ability, and unlike
    /// Dark Shield/Poison Aura (which still implement IAbilityRarityStats
    /// returning 0 for consistency), this one skips the interface entirely
    /// since it's never looked up by rarity-indexed array logic anyway.
    /// </summary>
    [Serializable]
    public struct CenterOfTheUniverseStats
    {
        public float BaseSize;
        public float SizeScalingPerEnemy;
        public float ExecuteScalingPerEnemy;
        public float ExecuteThresholdPercent;
    }
}
