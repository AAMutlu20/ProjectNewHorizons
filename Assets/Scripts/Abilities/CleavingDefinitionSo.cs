using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Cleaving Attacks melee enchant's designer-editable rarity tiers.
    /// Create via: right-click Project → Create → Game/Abilities/CleavingAttacks
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/CleavingAttacks", fileName = "Enchant_CleavingAttacks")]
    public class CleavingDefinitionSo : ScriptableObject
    {
        private const int RarityTierCount = 4;

        public string displayName = "Cleaving Attacks";
        public CleavingStats[] statsByRarity = new CleavingStats[RarityTierCount];

        public CleavingStats GetStatsForRarity(Stats.Rarity rarity)
        {
            var tierIndex = (int)rarity;
            if (tierIndex < 0 || tierIndex >= statsByRarity.Length)
            {
                Debug.LogError($"CleavingDefinitionSo: no stats for rarity {rarity}.", this);
                return default;
            }
            return statsByRarity[tierIndex];
        }

        private void Reset()
        {
            statsByRarity = new[]
            {
                new CleavingStats { BonusDamage = 30f, BleedDamagePercentPerSecond = 0.005f, BleedDuration = 1f, AttacksPerTrigger = 4, WeakenMultiplierBonus = 0f },
                new CleavingStats { BonusDamage = 40f, BleedDamagePercentPerSecond = 0.006f, BleedDuration = 2f, AttacksPerTrigger = 3, WeakenMultiplierBonus = 0f },
                new CleavingStats { BonusDamage = 50f, BleedDamagePercentPerSecond = 0.01f,  BleedDuration = 3f, AttacksPerTrigger = 2, WeakenMultiplierBonus = 0f },
                new CleavingStats { BonusDamage = 80f, BleedDamagePercentPerSecond = 0.02f,  BleedDuration = 5f, AttacksPerTrigger = 1, WeakenMultiplierBonus = 0.15f },
            };
        }
    }
}
