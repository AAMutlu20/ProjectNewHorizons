using Player;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Exposes Dark Shield as an IAbilityChoiceEntry. Dedicated rather than
    /// using the generic CooldownAbilityChoiceEntry, since Dark Shield is
    /// persistent (no Cast/cooldown) and granted via its own GrantShield
    /// method with its own stat shape.
    ///
    /// Attach to: a GameObject in [Systems], alongside the ability roster.
    /// </summary>
    public class DarkShieldChoiceEntry : MonoBehaviour, IAbilityChoiceEntry
    {
        [SerializeField] private DarkShieldDefinitionSo definition;
        [SerializeField] private Sprite icon;
        [SerializeField] private DarkShieldController shieldController;

        public string DisplayName => definition.displayName;
        public Sprite Icon => icon;

        private void Awake()
        {
            Debug.Assert(definition, $"{name}: definition not assigned.", this);
            Debug.Assert(shieldController, $"{name}: shieldController not assigned.", this);
        }

        public bool IsAvailableAtRarity(Rarity rarity) => true;

        public string GetDescription(Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            return $"Layers: {stats.Layers}  Layer Regen: {stats.LayerRegenSeconds:F0}s  Shield Regen: {stats.FullShieldRegenSeconds:F0}s";
        }

        public void Grant(Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            shieldController.GrantShield(stats);
        }
    }
}
