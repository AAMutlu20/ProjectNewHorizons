using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Generic wrapper exposing any 4-tier, cooldown-cast IAbility (Shockwave,
    /// Meteor Slam, Laser Beam, Cone of Fire -- anything built on
    /// AbilityDefinitionSo&lt;TStats&gt; and granted via PlayerAbilityManager)
    /// as an IAbilityChoiceEntry. One generic class covers all of them
    /// instead of writing near-identical wrapper boilerplate per ability.
    ///
    /// Persistent abilities (Dark Shield, Poison Aura, Center of the
    /// Universe) do NOT use this -- they have their own GrantXxx methods
    /// and stat shapes, so each gets its own small dedicated wrapper instead.
    ///
    /// Attach to: a GameObject in [Systems] alongside the ability roster --
    /// one of these per cooldown-cast ability, each wired to that ability's
    /// definition asset, the constructed IAbility instance, and PlayerAbilityManager.
    /// </summary>
    public class CooldownAbilityChoiceEntry<TStats> : MonoBehaviour, IAbilityChoiceEntry
        where TStats : IAbilityRarityStats
    {
        [SerializeField] private AbilityDefinitionSo<TStats> definition;
        [SerializeField] private Sprite icon;
        [SerializeField] private PlayerAbilityManager abilityManager;

        // Set via Configure() rather than the Inspector, since the concrete
        // IAbility instance (e.g. new ShockwaveAbility(definition)) typically
        // needs scene-specific pool references constructed in code, not
        // serializable as a plain field.
        private IAbility _ability;

        // Optional per-ability formatter for the choice card's description --
        // without one, GetDescription falls back to a generic cooldown-only
        // string, since that's the only field every TStats is guaranteed to
        // have. Each ability's actual numbers (Damage, Radius, etc.) need
        // ability-specific knowledge a generic wrapper can't have on its own.
        private System.Func<TStats, string> _describeStats;

        public string DisplayName => definition.displayName;
        public Sprite Icon => icon;

        private void Awake()
        {
            Debug.Assert(definition, $"{name}: definition not assigned.", this);
            Debug.Assert(abilityManager, $"{name}: abilityManager not assigned.", this);
        }

        /// <summary>
        /// Call once at scene setup with the constructed IAbility instance,
        /// and optionally a function turning this ability's stats into a
        /// player-facing description (e.g. stats => $"Damage: {stats.Damage}
        /// Radius: {stats.Radius}"). Without one, GetDescription shows only
        /// the cooldown.
        /// </summary>
        public void Configure(IAbility ability, System.Func<TStats, string> describeStats = null)
        {
            _ability = ability;
            _describeStats = describeStats;
        }

        public bool IsAvailableAtRarity(Rarity rarity) => true; // all 4 tiers exist for every ability using this wrapper

        public string GetDescription(Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            return _describeStats != null ? _describeStats(stats) : $"Cooldown: {stats.Cooldown:F1}s";
        }

        public void Grant(Rarity rarity)
        {
            if (_ability == null)
            {
                Debug.LogError($"{name}: Configure() was never called -- cannot grant '{DisplayName}'.", this);
                return;
            }

            var stats = definition.GetStatsForRarity(rarity);
            abilityManager.GrantOrUpgradeAbility(DisplayName, _ability, stats, rarity, icon);
        }
    }
}
