using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Designer-tunable odds for rolling a rarity. Used by LevelUpChoiceGenerator
    /// for normal level-ups (Legendary weight ignored — see RollExcludingLegendary)
    /// and boss rewards (all four tiers in play).
    ///
    /// Create via: right-click Project → Create → Game/Stats/RarityWeightTable
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Stats/RarityWeightTable", fileName = "RarityWeights_Default")]
    public class RarityWeightTableSo : ScriptableObject
    {
        [Header("Relative weights — higher rolls more often. Need not sum to 100.")]
        public float commonWeight = 60f;
        public float rareWeight = 25f;
        public float epicWeight = 12f;
        public float legendaryWeight = 3f;

        /// <summary>Rolls any rarity, including Legendary. Use for boss-kill rewards.</summary>
        public Rarity RollAnyRarity()
        {
            return RollFromWeights(commonWeight, rareWeight, epicWeight, legendaryWeight);
        }

        /// <summary>Rolls Common/Rare/Epic only. Use for normal level-up rewards.</summary>
        public Rarity RollExcludingLegendary()
        {
            return RollFromWeights(commonWeight, rareWeight, epicWeight, 0f);
        }

        private static Rarity RollFromWeights(float common, float rare, float epic, float legendary)
        {
            var totalWeight = common + rare + epic + legendary;
            if (totalWeight <= 0f)
            {
                Debug.LogError("RarityWeightTableSo: all weights are zero, defaulting to Common.");
                return Rarity.Common;
            }

            var roll = Random.Range(0f, totalWeight);

            if (roll < common) return Rarity.Common;
            roll -= common;

            if (roll < rare) return Rarity.Rare;
            roll -= rare;

            if (roll < epic) return Rarity.Epic;

            return Rarity.Legendary;
        }
    }
}
