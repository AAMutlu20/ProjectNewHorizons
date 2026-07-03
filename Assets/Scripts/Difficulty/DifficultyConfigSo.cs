using UnityEngine;

namespace Difficulty
{
    /// <summary>
    /// ScriptableObject driving DifficultyScaler. Curves are keyed by elapsed
    /// game time (normalised against maxScalingTimeSeconds) rather than a
    /// wave index, since the cycle repeats indefinitely and time is the only
    /// value that keeps climbing.
    ///
    /// Create via: right-click Project → Create → Game/Difficulty/DifficultyConfig
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Difficulty/DifficultyConfig", fileName = "DifficultyConfig_Default")]
    public class DifficultyConfigSo : ScriptableObject
    {
        [Header("Time scaling — X axis is 0..1 (elapsed time / maxScalingTimeSeconds)")]
        [Tooltip("Seconds of elapsed game time at which the curves below reach their final (X=1) value. " +
                 "Past this point, values stay flat at the curve's end — the cycle keeps escalating via " +
                 "time scaling reaches its cap instead of this curve climbing further.")]
        public float maxScalingTimeSeconds = 1800f; // 30 minutes

        [Tooltip("How much more HP enemies have as time passes")]
        public AnimationCurve enemyHpMultiplier = AnimationCurve.Linear(0, 1, 1, 4);

        [Tooltip("How much more damage enemies deal as time passes")]
        public AnimationCurve enemyDamageMultiplier = AnimationCurve.Linear(0, 1, 1, 2);

        [Tooltip("Simultaneous enemy budget — ramps up then plateaus")]
        public AnimationCurve activeBudget = AnimationCurve.EaseInOut(0, 20, 1, 150);

        [Header("Spawn radius — how far from centre enemies appear")]
        public float minSpawnRadius = 12f;
        public float maxSpawnRadius = 18f;
    }
}
