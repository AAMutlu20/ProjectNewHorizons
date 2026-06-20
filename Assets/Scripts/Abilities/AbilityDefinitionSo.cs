using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Base for every ability's ScriptableObject definition. TStats is that
    /// ability's own per-rarity stat struct (e.g. ShockwaveStats with Damage,
    /// Radius, StunTime) — each ability defines its own, since they don't
    /// share a common stat shape beyond Cooldown (see IAbilityRarityStats).
    ///
    /// Concrete abilities derive from this rather than duplicating the
    /// rarity-lookup boilerplate eight times.
    /// </summary>
    public abstract class AbilityDefinitionSo<TStats> : ScriptableObject where TStats : IAbilityRarityStats
    {
        private const int RarityTierCount = 4; // Common, Rare, Epic, Legendary

        [Header("Identity")]
        public string displayName = "New Ability";

        [Tooltip("Stats per rarity tier, in array order: Common, Rare, Epic, Legendary.")]
        public TStats[] statsByRarity = new TStats[RarityTierCount];

        public TStats GetStatsForRarity(Stats.Rarity rarity)
        {
            var tierIndex = (int)rarity;

            if (tierIndex < 0 || tierIndex >= statsByRarity.Length)
            {
                Debug.LogError(
                    $"{GetType().Name} '{name}': no stats configured for rarity {rarity}. " +
                    $"statsByRarity must have {RarityTierCount} entries.", this);
                return default;
            }

            return statsByRarity[tierIndex];
        }

        private void OnValidate()
        {
            if (statsByRarity.Length == RarityTierCount) return;

            Debug.LogWarning(
                $"{GetType().Name} '{name}': statsByRarity should have exactly " +
                $"{RarityTierCount} entries (Common, Rare, Epic, Legendary), found {statsByRarity.Length}.",
                this);
        }
    }
}
