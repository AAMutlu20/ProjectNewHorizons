using System.Collections.Generic;
using Core;
using Stats;
using UnityEngine;
using XP;

namespace UI
{
    /// <summary>
    /// Shows the level-up stat choice screen. Converts each StatChoiceOption
    /// into a ChoiceCardData and hands it to a pooled set of ChoiceCardWidget
    /// instances — this is the ONLY file that knows StatChoiceOption exists.
    /// A future AbilityChoiceScreenController would do the equivalent
    /// conversion for abilities; neither needs to know about the other, and
    /// neither ChoiceCardWidget nor the screen layout needs to change for
    /// either to work.
    ///
    /// Attach to: a Canvas panel, initially inactive. Wire cardSlots to
    /// exactly 3 pre-placed ChoiceCardWidget instances in the layout (cards
    /// are reused, not Instantiate'd, since the count is fixed at 3 per the
    /// design doc).
    /// </summary>
    public class StatChoiceScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private List<ChoiceCardWidget> cardSlots = new();
        [SerializeField] private LevelSystem levelSystem;

        private void Awake()
        {
            Debug.Assert(screenRoot, "StatChoiceScreenController: screenRoot not assigned.", this);
            Debug.Assert(levelSystem, "StatChoiceScreenController: levelSystem not assigned.", this);

            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StatChoicePresentedEvent>(OnStatChoicePresented);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StatChoicePresentedEvent>(OnStatChoicePresented);
        }

        private void OnStatChoicePresented(StatChoicePresentedEvent choicePresented)
        {
            BindCardsToChoices(choicePresented.Choices);
            SetVisible(true);
        }

        private void BindCardsToChoices(List<StatChoiceOption> choices)
        {
            var cardCount = Mathf.Min(cardSlots.Count, choices.Count);

            if (choices.Count != cardSlots.Count)
                Debug.LogWarning($"StatChoiceScreenController: received {choices.Count} choices but " +
                                  $"have {cardSlots.Count} card slots — showing {cardCount}.", this);

            for (var i = 0; i < cardCount; i++)
                cardSlots[i].Bind(ConvertToCardData(choices[i]));
        }

        private ChoiceCardData ConvertToCardData(StatChoiceOption choice)
        {
            var modifier = choice.Modifier;
            var definition = modifier.Definition;

            var valueText = definition.modifierType == StatModifierType.PercentAdditive
                ? $"+{modifier.Value}%"
                : $"+{modifier.Value}";

            return new ChoiceCardData(
                title: definition.displayName,
                description: valueText,
                rarity: modifier.Rarity,
                icon: null, // no icon assets exist yet — wire once art is available
                onSelected: () => HandleCardSelected(modifier));
        }

        private void HandleCardSelected(StatModifier chosenModifier)
        {
            levelSystem.ResolveStatChoice(chosenModifier);
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            screenRoot.SetActive(isVisible);
        }
    }
}
