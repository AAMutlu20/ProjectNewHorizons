using Player;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Exposes Center of the Universe as an IAbilityChoiceEntry. This is the
    /// one ability where IsAvailableAtRarity actually does real work -- per
    /// the design doc, it only exists at Epic/Legendary, and
    /// CenterOfTheUniverseDefinitionSo.GetStats only has data for those two
    /// tiers (it logs an error and falls back to Epic for anything else,
    /// which is a defensive measure, not something callers should rely on --
    /// the generator must filter this entry out at Common/Rare BEFORE ever
    /// calling GetStats with those rarities).
    ///
    /// Attach to: a GameObject in [Systems], alongside the ability roster.
    /// </summary>
    public class CenterOfTheUniverseChoiceEntry : MonoBehaviour, IAbilityChoiceEntry
    {
        [SerializeField] private CenterOfTheUniverseDefinitionSo definition;
        [SerializeField] private Sprite icon;
        [SerializeField] private CenterOfTheUniverseController blackHoleController;

        public string DisplayName => definition.displayName;
        public Sprite Icon => icon;

        private void Awake()
        {
            Debug.Assert(definition, $"{name}: definition not assigned.", this);
            Debug.Assert(blackHoleController, $"{name}: blackHoleController not assigned.", this);
        }

        public bool IsAvailableAtRarity(Rarity rarity) => rarity == Rarity.Epic || rarity == Rarity.Legendary;

        public string GetDescription(Rarity rarity)
        {
            var stats = definition.GetStats(rarity);
            return $"Size: {stats.BaseSize:F0}  Execute: {stats.ExecuteThresholdPercent:F0}% maxHP";
        }

        public void Grant(Rarity rarity)
        {
            if (!IsAvailableAtRarity(rarity))
            {
                Debug.LogError($"CenterOfTheUniverseChoiceEntry: attempted grant at unsupported rarity {rarity}.", this);
                return;
            }

            var stats = definition.GetStats(rarity);
            blackHoleController.GrantBlackHole(stats);
        }
    }
}
