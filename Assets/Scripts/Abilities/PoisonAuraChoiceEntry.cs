using Player;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Exposes Poison Aura as an IAbilityChoiceEntry. Dedicated wrapper for
    /// the same reason as DarkShieldChoiceEntry -- persistent, own Grant
    /// method, own stat shape.
    ///
    /// Attach to: a GameObject in [Systems], alongside the ability roster.
    /// </summary>
    public class PoisonAuraChoiceEntry : MonoBehaviour, IAbilityChoiceEntry
    {
        [SerializeField] private PoisonAuraDefinitionSo definition;
        [SerializeField] private Sprite icon;
        [SerializeField] private PoisonAuraController auraController;

        public string DisplayName => definition.displayName;
        public Sprite Icon => icon;

        private void Awake()
        {
            Debug.Assert(definition, $"{name}: definition not assigned.", this);
            Debug.Assert(auraController, $"{name}: auraController not assigned.", this);
        }

        public bool IsAvailableAtRarity(Rarity rarity) => true;

        public string GetDescription(Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            var slowText = stats.SlowFraction > 0f ? $"  Slow: {stats.SlowFraction * 100f:F0}%" : "";
            return $"Damage: {stats.Damage:F0}  Frequency: {stats.DamageFrequencySeconds:F1}s  Range: {stats.Range:F0}{slowText}";
        }

        public void Grant(Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            auraController.GrantAura(stats);
        }
    }
}
