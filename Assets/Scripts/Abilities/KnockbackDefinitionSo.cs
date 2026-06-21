using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Knockback melee enchant's designer-editable rarity tiers. Not built
    /// on AbilityDefinitionSo&lt;TStats&gt; — that base assumes a Cooldown
    /// field via IAbilityRarityStats, which this enchant has no use for
    /// (it's not cast at all, just a passive force added to every melee hit).
    ///
    /// Create via: right-click Project → Create → Game/Abilities/Knockback
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/Knockback", fileName = "Enchant_Knockback")]
    public class KnockbackDefinitionSo : ScriptableObject
    {
        private const int RarityTierCount = 4;

        public string displayName = "Knockback";
        public KnockbackStats[] statsByRarity = new KnockbackStats[RarityTierCount];

        public KnockbackStats GetStatsForRarity(Stats.Rarity rarity)
        {
            var tierIndex = (int)rarity;
            if (tierIndex < 0 || tierIndex >= statsByRarity.Length)
            {
                Debug.LogError($"KnockbackDefinitionSo: no stats for rarity {rarity}.", this);
                return default;
            }
            return statsByRarity[tierIndex];
        }

        private void Reset()
        {
            statsByRarity = new[]
            {
                new KnockbackStats { KnockbackForce = 4f },
                new KnockbackStats { KnockbackForce = 5f },
                new KnockbackStats { KnockbackForce = 6f },
                new KnockbackStats { KnockbackForce = 8f },
            };
        }
    }
}
