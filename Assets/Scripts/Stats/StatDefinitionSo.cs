using System;
using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Designer-authored definition of one stat: its display name, how its
    /// value combines with the base stat (flat or percent), and the value
    /// granted at each rarity tier.
    ///
    /// Create via: right-click Project → Create → Game/Stats/StatDefinition
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Stats/StatDefinition", fileName = "Stat_")]
    public class StatDefinitionSo : ScriptableObject
    {
        private const int RarityTierCount = 4; // Common, Rare, Epic, Legendary

        [Header("Identity")]
        public StatType statType;
        public string displayName = "New Stat";

        [Tooltip("Icon shown on the choice card. Assign any Sprite in the Project window. " +
                 "Leave null to show no icon (the card still works fine without one).")]
        public UnityEngine.Sprite icon;

        [Header("Scaling")]
        public StatModifierType modifierType = StatModifierType.Flat;

        [Tooltip("Value granted per rarity tier, in array order: Common, Rare, Epic, Legendary.")]
        public float[] valueByRarity = new float[RarityTierCount];

        public float GetValueForRarity(Rarity rarity)
        {
            var tierIndex = (int)rarity;

            if (tierIndex < 0 || tierIndex >= valueByRarity.Length)
            {
                Debug.LogError(
                    $"StatDefinitionSo '{name}': no value configured for rarity {rarity}. " +
                    $"valueByRarity must have {RarityTierCount} entries.", this);
                return 0f;
            }

            return valueByRarity[tierIndex];
        }

        private void OnValidate()
        {
            if (valueByRarity.Length == RarityTierCount) return;

            Debug.LogWarning(
                $"StatDefinitionSo '{name}': valueByRarity should have exactly " +
                $"{RarityTierCount} entries (Common, Rare, Epic, Legendary), found {valueByRarity.Length}.",
                this);
        }
    }
}
