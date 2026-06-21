using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Center of the Universe's designer-editable tiers — Epic and Legendary
    /// only, per the design doc. Whatever rolls ability choices for the
    /// level-up/boss-reward UI must never offer this at Common/Rare; that's
    /// a choice-generator responsibility, not enforced by this asset.
    ///
    /// Create via: right-click Project → Create → Game/Abilities/CenterOfTheUniverse
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/CenterOfTheUniverse", fileName = "Ability_CenterOfTheUniverse")]
    public class CenterOfTheUniverseDefinitionSo : ScriptableObject
    {
        public string displayName = "Center of the Universe";

        public CenterOfTheUniverseStats epicStats;
        public CenterOfTheUniverseStats legendaryStats;

        public CenterOfTheUniverseStats GetStats(Stats.Rarity rarity)
        {
            switch (rarity)
            {
                case Stats.Rarity.Epic:
                    return epicStats;
                case Stats.Rarity.Legendary:
                    return legendaryStats;
                default:
                    Debug.LogError($"CenterOfTheUniverseDefinitionSo: rarity {rarity} has no stats — " +
                                    "this ability is Epic/Legendary only. Returning Epic stats as a fallback.", this);
                    return epicStats;
            }
        }

        private void Reset()
        {
            epicStats = new CenterOfTheUniverseStats
            {
                BaseSize = 6f, SizeScalingPerEnemy = 0.1f,
                ExecuteScalingPerEnemy = 0.05f, ExecuteThresholdPercent = 5f,
            };
            legendaryStats = new CenterOfTheUniverseStats
            {
                BaseSize = 8f, SizeScalingPerEnemy = 0.2f,
                ExecuteScalingPerEnemy = 0.05f, ExecuteThresholdPercent = 8f,
            };
        }
    }
}
