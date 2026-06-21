using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Lifesteal melee enchant's designer-editable rarity tiers.
    /// Create via: right-click Project → Create → Game/Abilities/Lifesteal
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/Lifesteal", fileName = "Enchant_Lifesteal")]
    public class LifestealDefinitionSo : ScriptableObject
    {
        private const int RarityTierCount = 4;

        public string displayName = "Lifesteal";
        public LifestealStats[] statsByRarity = new LifestealStats[RarityTierCount];

        public LifestealStats GetStatsForRarity(Stats.Rarity rarity)
        {
            var tierIndex = (int)rarity;
            if (tierIndex < 0 || tierIndex >= statsByRarity.Length)
            {
                Debug.LogError($"LifestealDefinitionSo: no stats for rarity {rarity}.", this);
                return default;
            }
            return statsByRarity[tierIndex];
        }

        private void Reset()
        {
            statsByRarity = new[]
            {
                new LifestealStats { DamageHealPercent = 6f,  PermanentHpIncreasePerHit = 0f },
                new LifestealStats { DamageHealPercent = 14f, PermanentHpIncreasePerHit = 0f },
                new LifestealStats { DamageHealPercent = 25f, PermanentHpIncreasePerHit = 0f },
                new LifestealStats { DamageHealPercent = 35f, PermanentHpIncreasePerHit = 0.01f },
            };
        }
    }
}
