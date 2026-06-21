using System;
using UnityEngine;

namespace Abilities
{
    /// <summary>Per-rarity stats for the Lifesteal melee enchant, matching the design doc exactly.</summary>
    [Serializable]
    public struct LifestealStats
    {
        public float DamageHealPercent;

        [Tooltip("Legendary only — permanent flat HP gained per landed hit. 0 at every other tier.")]
        public float PermanentHpIncreasePerHit;
    }
}
