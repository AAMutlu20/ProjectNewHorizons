using System.Collections.Generic;
using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Rolls sets of stat choices for level-up and boss-reward screens.
    /// Pure logic — no MonoBehaviour — so it's easy to call from whichever
    /// system owns the level-up flow (e.g. a future LevelSystem).
    /// </summary>
    public class LevelUpChoiceGenerator
    {
        private const int ChoiceCount = 3;

        private readonly StatPoolSo _statPool;
        private readonly RarityWeightTableSo _rarityWeights;

        // Reused across rolls to avoid allocating a new list every level-up.
        private readonly List<StatDefinitionSo> _availableThisRoll = new();

        public LevelUpChoiceGenerator(StatPoolSo statPool, RarityWeightTableSo rarityWeights)
        {
            _statPool = statPool;
            _rarityWeights = rarityWeights;
        }

        /// <summary>Rolls 3 unique stats at Common/Rare/Epic rarity. Used for normal level-ups.</summary>
        public List<StatChoiceOption> RollLevelUpChoices()
        {
            return RollChoices(_rarityWeights.RollExcludingLegendary);
        }

        /// <summary>Rolls 3 unique stats including Legendary. Used for boss-kill rewards.</summary>
        public List<StatChoiceOption> RollBossRewardChoices()
        {
            return RollChoices(_rarityWeights.RollAnyRarity);
        }

        private List<StatChoiceOption> RollChoices(System.Func<Rarity> rollRarity)
        {
            var choices = new List<StatChoiceOption>(ChoiceCount);

            if (!HasEnoughStatsToOfferChoices()) return choices;

            RefillAvailableStats();

            for (var i = 0; i < ChoiceCount; i++)
            {
                var statDefinition = PickAndRemoveRandomStat();
                var rarity = rollRarity();
                var modifier = new StatModifier(statDefinition, rarity);
                choices.Add(new StatChoiceOption(modifier));
            }

            return choices;
        }

        private bool HasEnoughStatsToOfferChoices()
        {
            if (_statPool != null && _statPool.availableStats.Length >= ChoiceCount) return true;

            Debug.LogError(
                $"LevelUpChoiceGenerator: StatPool must contain at least {ChoiceCount} stats " +
                "to offer a full set of choices.");
            return false;
        }

        private void RefillAvailableStats()
        {
            _availableThisRoll.Clear();
            _availableThisRoll.AddRange(_statPool.availableStats);
        }

        private StatDefinitionSo PickAndRemoveRandomStat()
        {
            var index = Random.Range(0, _availableThisRoll.Count);
            var statDefinition = _availableThisRoll[index];
            _availableThisRoll.RemoveAt(index);
            return statDefinition;
        }
    }
}
