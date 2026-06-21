using System;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Per-rarity stats for the Cleaving Attacks melee enchant, matching the
    /// design doc exactly. AttacksPerTrigger is "once every N attacks" —
    /// Legendary's "every attack is a cleaving attack" is represented as
    /// AttacksPerTrigger = 1, not a separate flag.
    /// </summary>
    [Serializable]
    public struct CleavingStats
    {
        public float BonusDamage;

        [Tooltip("Percent of the TARGET's own max HP, dealt per second for BleedDuration.")]
        public float BleedDamagePercentPerSecond;
        public float BleedDuration;

        [Tooltip("Cleaving triggers once every N swings. 1 = every swing (Legendary).")]
        public int AttacksPerTrigger;

        [Tooltip("Legendary only — weaken multiplier bonus applied to hit enemies. 0 at every other tier.")]
        public float WeakenMultiplierBonus;
    }
}
