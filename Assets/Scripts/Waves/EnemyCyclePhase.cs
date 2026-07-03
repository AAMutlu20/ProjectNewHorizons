using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// One phase within the enemy-introduction cycle — e.g. "Spiders unlock,
    /// starting at 2-3, ramping up over the phase." A full cycle is an
    /// ordered list of these (see CycleConfigSo), ending in a boss phase.
    /// </summary>
    [System.Serializable]
    public class EnemyCyclePhase
    {
        [Tooltip("Which enemy type unlocks when this phase begins. Ignored for the boss phase.")]
        public EnemyType enemyType = EnemyType.Zombie;

        [Tooltip("How many of this type spawn the instant the phase begins.")]
        public int startingCount = 3;

        [Tooltip("Seconds between consecutive spawns of this type once the phase is active.")]
        public float spawnInterval = 1.5f;

        [Tooltip("Base seconds this phase lasts before the next phase begins. " +
                 "Reduced over time by CycleEscalation.PhaseIntervalReduction.")]
        public float baseDuration = 60f;

        [Tooltip("How much the spawn count per interval grows over the phase's duration — " +
                 "1 = flat rate, 2 = doubles by the end of the phase.")]
        public AnimationCurve countRampOverPhase = AnimationCurve.Linear(0, 1, 1, 2);

    }
}
