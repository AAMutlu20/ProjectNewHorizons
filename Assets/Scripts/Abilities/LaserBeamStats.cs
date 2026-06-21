using System;
using UnityEngine;

namespace Abilities
{
    /// <summary>Per-rarity stats for Laser Beam, matching the design doc exactly.</summary>
    [Serializable]
    public struct LaserBeamStats : IAbilityRarityStats
    {
        public int BeamCount;

        [Tooltip("Fraction added to damage taken from ALL sources while weakened, e.g. 0.10 = 10% more.")]
        public float WeakeningMultiplierBonus;

        [Tooltip("Damage per second as a fraction of each affected enemy's own max HP.")]
        public float DamagePercentMaxHpPerSecond;

        public float Duration;
        public float Cooldown;

        float IAbilityRarityStats.Cooldown => Cooldown;
    }
}
