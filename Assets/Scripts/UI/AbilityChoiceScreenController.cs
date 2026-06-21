using System.Collections.Generic;
using Abilities;
using Core;
using UnityEngine;
using XP;

namespace UI
{
    /// <summary>
    /// Shows the every-3rd-level ability choice screen. Converts each
    /// AbilityChoiceOption into a ChoiceCardData and hands it to a pooled
    /// set of ChoiceCardWidget instances -- the SAME ChoiceCardWidget and
    /// ChoiceCardData used by StatChoiceScreenController. This is the ONLY
    /// file that knows AbilityChoiceOption exists; neither it nor
    /// StatChoiceScreenController need to know about each other.
    ///
    /// Attach to: a Canvas panel, initially inactive, separate from the
    /// stat choice screen panel (only one of the two is ever shown for a
    /// given level-up, per LevelUpEvent.IsAbilityLevel). Wire cardSlots to
    /// exactly 3 pre-placed ChoiceCardWidget instances.
    /// </summary>
    public class AbilityChoiceScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private List<ChoiceCardWidget> cardSlots = new();
        [SerializeField] private LevelSystem levelSystem;

        private void Awake()
        {
            Debug.Assert(screenRoot, "AbilityChoiceScreenController: screenRoot not assigned.", this);
            Debug.Assert(levelSystem, "AbilityChoiceScreenController: levelSystem not assigned.", this);

            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AbilityChoicePresentedEvent>(OnAbilityChoicePresented);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AbilityChoicePresentedEvent>(OnAbilityChoicePresented);
        }

        private void OnAbilityChoicePresented(AbilityChoicePresentedEvent choicePresented)
        {
            BindCardsToChoices(choicePresented.Choices);
            SetVisible(true);
        }

        private void BindCardsToChoices(List<AbilityChoiceOption> choices)
        {
            var cardCount = Mathf.Min(cardSlots.Count, choices.Count);

            if (choices.Count != cardSlots.Count)
                Debug.LogWarning($"AbilityChoiceScreenController: received {choices.Count} choices but " +
                                  $"have {cardSlots.Count} card slots -- showing {cardCount}.", this);

            for (var i = 0; i < cardCount; i++)
                cardSlots[i].Bind(ConvertToCardData(choices[i]));
        }

        private ChoiceCardData ConvertToCardData(AbilityChoiceOption choice)
        {
            return new ChoiceCardData(
                title: choice.Entry.DisplayName,
                description: choice.Entry.GetDescription(choice.Rarity),
                rarity: choice.Rarity,
                icon: choice.Entry.Icon,
                onSelected: () => HandleCardSelected(choice));
        }

        private void HandleCardSelected(AbilityChoiceOption chosenOption)
        {
            levelSystem.ResolveAbilityChoice(chosenOption);
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            screenRoot.SetActive(isVisible);
        }
    }
}
