using System;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Per-rarity stats for Meteor Slam, matching the design doc exactly.
    /// LavaPoolDamagePercentPerSecond/Range/Duration are all 0 at Base and
    /// Rare — the lava pool only exists from Epic onward, not a separate
    /// on/off flag.
    /// </summary>
    [Serializable]
    public struct MeteorSlamStats : IAbilityRarityStats
    {
        public float Damage;
        public float ImpactRadius;
        public float Cooldown;

        [Tooltip("Damage per second as a fraction of EACH AFFECTED ENEMY'S OWN max HP, " +
                 "not a flat number — e.g. 0.10 means 10% of that enemy's max HP per second.")]
        public float LavaPoolDamagePercentPerSecond;
        public float LavaPoolRange;
        public float LavaPoolDuration;

        float IAbilityRarityStats.Cooldown => Cooldown;
    }
}
