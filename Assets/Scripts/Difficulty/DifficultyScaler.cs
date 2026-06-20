using UnityEngine;
using Waves;

namespace Difficulty
{
    /// <summary>
    /// Computes a DifficultyParams for the current moment, given elapsed
    /// game time and the cycle's boss-kill escalation. Pure function — no
    /// state of its own beyond the config reference.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class DifficultyScaler : MonoBehaviour
    {
        [SerializeField] private DifficultyConfigSo config;

        private void Awake()
        {
            Debug.Assert(config, "DifficultyScaler: DifficultyConfig asset not assigned", this);
        }

        /// <summary>
        /// Returns a fully resolved DifficultyParams for the given elapsed
        /// game time and escalation state. Call this whenever a spawn needs
        /// fresh difficulty values — typically once per phase transition,
        /// not necessarily every frame.
        /// </summary>
        public DifficultyParams Scale(float elapsedGameTimeSeconds, CycleEscalation escalation)
        {
            var normalisedTime = config.maxScalingTimeSeconds > 0f
                ? Mathf.Clamp01(elapsedGameTimeSeconds / config.maxScalingTimeSeconds)
                : 0f;

            var radius = Mathf.Lerp(config.minSpawnRadius, config.maxSpawnRadius, normalisedTime);
            var escalationBonus = escalation.StatBonusMultiplier;

            return new DifficultyParams
            {
                HpMult = config.enemyHpMultiplier.Evaluate(normalisedTime) * escalationBonus,
                DamageMult = config.enemyDamageMultiplier.Evaluate(normalisedTime) * escalationBonus,
                SpawnRadius = radius,
                Budget = config.activeBudget.Evaluate(normalisedTime),
            };
        }
    }
}
