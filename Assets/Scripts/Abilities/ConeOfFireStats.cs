using System;
using UnityEngine;

namespace Abilities
{
    /// <summary>Per-rarity stats for Cone of Fire, matching the design doc exactly.</summary>
    [Serializable]
    public struct ConeOfFireStats : IAbilityRarityStats
    {
        public float Damage;
        public float Range;
        public float Cooldown;

        [Tooltip("Damage PER SECOND for ResidualDuration — not a one-time total.")]
        public float ResidualDamagePerSecond;
        public float ResidualDuration;

        float IAbilityRarityStats.Cooldown => Cooldown;
    }
}
