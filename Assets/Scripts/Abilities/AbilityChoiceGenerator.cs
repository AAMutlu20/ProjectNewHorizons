using System.Collections.Generic;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Rolls sets of ability choices for the every-3rd-level ability screen
    /// and boss-reward screens. Pure logic -- no MonoBehaviour -- mirrors
    /// LevelUpChoiceGenerator's shape.
    ///
    /// Rarity is rolled FIRST, then the candidate pool is filtered to
    /// abilities actually available at that rarity (IsAvailableAtRarity) --
    /// this guarantees validity by construction rather than rolling an
    /// (ability, rarity) pair and rejecting invalid combinations, which
    /// could need an unbounded number of retries if a roll kept landing on
    /// Center of the Universe at Common.
    /// </summary>
    public class AbilityChoiceGenerator
    {
        private const int ChoiceCount = 3;

        private readonly AbilityRoster _roster;
        private readonly RarityWeightTableSo _rarityWeights;

        // Reused across rolls to avoid allocating new lists every level-up.
        private readonly List<IAbilityChoiceEntry> _candidatesThisRoll = new();

        public AbilityChoiceGenerator(AbilityRoster roster, RarityWeightTableSo rarityWeights)
        {
            _roster = roster;
            _rarityWeights = rarityWeights;
        }

        /// <summary>Rolls 3 unique abilities at Common/Rare/Epic rarity. Used for the every-3rd-level ability screen.</summary>
        public List<AbilityChoiceOption> RollLevelUpChoices()
        {
            return RollChoices(_rarityWeights.RollExcludingLegendary);
        }

        /// <summary>Rolls 3 unique abilities including Legendary. Used for cycle-end rewards.</summary>
        public List<AbilityChoiceOption> RollLevelUpChoicesLegendary()
        {
            return RollChoices(_rarityWeights.RollAnyRarity);
        }


        private List<AbilityChoiceOption> RollChoices(System.Func<Rarity> rollRarity)
        {
            var choices = new List<AbilityChoiceOption>(ChoiceCount);
            var entries = _roster.GetEntries();

            if (entries.Length < ChoiceCount)
            {
                Debug.LogError($"AbilityChoiceGenerator: roster has only {entries.Length} entries, " +
                                $"need at least {ChoiceCount} to offer a full set of choices.");
                return choices;
            }

            for (var i = 0; i < ChoiceCount; i++)
            {
                var option = RollOneChoiceExcluding(entries, choices, rollRarity);
                if (option.HasValue) choices.Add(option.Value);
            }

            return choices;
        }

        /// <summary>
        /// Rolls a rarity, filters the roster to entries available at that
        /// rarity AND not already offered this roll, then picks one at random.
        /// </summary>
        private AbilityChoiceOption? RollOneChoiceExcluding(
            IAbilityChoiceEntry[] entries, List<AbilityChoiceOption> alreadyChosen, System.Func<Rarity> rollRarity)
        {
            var rarity = rollRarity();

            _candidatesThisRoll.Clear();
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (!entry.IsAvailableAtRarity(rarity)) continue;
                if (IsAlreadyChosen(entry, alreadyChosen)) continue;

                _candidatesThisRoll.Add(entry);
            }

            if (_candidatesThisRoll.Count == 0)
            {
                Debug.LogWarning($"AbilityChoiceGenerator: no available ability for rolled rarity {rarity} " +
                                  "(all eligible entries already chosen this roll, or none support this rarity).");
                return null;
            }

            var chosenEntry = _candidatesThisRoll[Random.Range(0, _candidatesThisRoll.Count)];
            return new AbilityChoiceOption(chosenEntry, rarity);
        }

        private static bool IsAlreadyChosen(IAbilityChoiceEntry entry, List<AbilityChoiceOption> alreadyChosen)
        {
            foreach (var chosen in alreadyChosen)
            {
                if (chosen.Entry == entry) return true;
            }
            return false;
        }
    }
}
