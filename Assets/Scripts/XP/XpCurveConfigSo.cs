using UnityEngine;

namespace XP
{
    /// <summary>
    /// Designer-tunable XP curves.
    ///
    /// xpRequiredPerLevel grows faster than enemyXpDropMultiplier over time —
    /// per the design doc, the XP needed to level up scales roughly 10x
    /// steeper than the XP enemies drop, so progression isn't linear.
    ///
    /// Create via: right-click Project → Create → Game/XP/XpCurveConfig
    /// </summary>
    [CreateAssetMenu(menuName = "Game/XP/XpCurveConfig", fileName = "XpCurveConfig_Default")]
    public class XpCurveConfigSo : ScriptableObject
    {
        [Header("XP required to reach the next level")]
        [Tooltip("X axis: level (normalised against levelCurveMaxLevel). Y axis: XP required.")]
        public AnimationCurve xpRequiredPerLevel = AnimationCurve.Linear(0, 10, 1, 500);
        public int levelCurveMaxLevel = 100;

        [Header("How much enemy XP drops scale with elapsed game time")]
        [Tooltip("X axis: 0..1 normalised against maxScalingTimeSeconds. Y axis: multiplier on an enemy's base XP drop.")]
        public AnimationCurve enemyXpDropMultiplier = AnimationCurve.Linear(0, 1, 1, 1.5f);
        public float maxScalingTimeSeconds = 1200f; // 20 minutes

        [Header("Miniboss XP rules")]
        public float minibossXpMultiplier = 1.5f;

        public float GetXpRequiredForLevel(int level)
        {
            var normalisedLevel = levelCurveMaxLevel > 0
                ? Mathf.Clamp01((float)level / levelCurveMaxLevel)
                : 0f;
            return xpRequiredPerLevel.Evaluate(normalisedLevel);
        }

        public float GetEnemyXpMultiplier(float elapsedSeconds)
        {
            var normalisedTime = maxScalingTimeSeconds > 0f
                ? Mathf.Clamp01(elapsedSeconds / maxScalingTimeSeconds)
                : 0f;
            return enemyXpDropMultiplier.Evaluate(normalisedTime);
        }

    }
}