using Core;
using UnityEngine;

namespace Difficulty
{
    /// <summary>
    /// Reads DifficultyConfig AnimationCurves and returns a DifficultyParams struct
    /// for a given wave index. Pure function - no state, no side effects.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class DifficultyScaler : MonoBehaviour
    {
        [SerializeField] private DifficultyConfig config;

        private void Awake()
        {
            Debug.Assert(config, "DifficultyScaler: DifficultyConfig asset not assigned");
        }

        /// <summary>
        /// Returns a fully resolved DifficultyParams for the given wave number.
        /// Call this at the start of each wave, before SpawnScheduler.LoadWave().
        /// </summary>
        public DifficultyParams Scale(int waveIndex)
        {
            var t = config.totalWaves > 0
                ? Mathf.Clamp01((float)waveIndex / config.totalWaves)
                : 0f;

            // Lerp spawn radius so enemies appear further away on later waves
            var radius = Mathf.Lerp(config.minSpawnRadius, config.maxSpawnRadius, t);

            var p = new DifficultyParams
            {
                CountMult = config.spawnCountMultiplier.Evaluate(t),
                SpeedMult = config.enemySpeedMultiplier.Evaluate(t),
                HpMult = config.enemyHpMultiplier.Evaluate(t),
                DamageMult = config.enemyDamageMultiplier.Evaluate(t),
                SpawnRadius = radius,
                Budget = config.activeBudget.Evaluate(t),
            };

            EventBus.Emit(new DifficultyChangedEvent
            {
                Wave = waveIndex,
                CountMult = p.CountMult,
                SpeedMult = p.SpeedMult,
            });

            return p;
        }
    }
}
