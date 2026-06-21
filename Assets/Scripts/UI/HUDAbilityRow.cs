using System.Collections.Generic;
using Abilities;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Drives the row of AbilitySlotWidget diamonds from PlayerAbilityManager.
    /// Polls rather than event-drives, since cooldown progress changes every
    /// frame anyway and there's no per-grant event to hook (granting an
    /// ability happens via direct method call from the choice screen, not
    /// an EventBus event — see StatChoiceScreenController for the analogous
    /// stat case).
    ///
    /// Slot count always matches PlayerAbilityManager.MaxAbilitySlots —
    /// unfilled slots render empty via AbilitySlotWidget.SetEmpty.
    ///
    /// Attach to: Desktop HUD canvas, the ability row container.
    /// </summary>
    public class HUDAbilityRow : MonoBehaviour
    {
        [SerializeField] private PlayerAbilityManager abilityManager;
        [SerializeField] private List<AbilitySlotWidget> slots = new();

        private void Awake()
        {
            Debug.Assert(abilityManager, "HUDAbilityRow: abilityManager not assigned.", this);

            if (slots.Count != PlayerAbilityManager.MaxAbilitySlots)
                Debug.LogWarning($"HUDAbilityRow: expected {PlayerAbilityManager.MaxAbilitySlots} slots, " +
                                  $"found {slots.Count} wired in the Inspector.", this);
        }

        private void Update()
        {
            var activeAbilities = abilityManager.ActiveAbilities;

            for (var i = 0; i < slots.Count; i++)
            {
                if (i < activeAbilities.Count)
                    slots[i].Bind(activeAbilities[i]);
                else
                    slots[i].SetEmpty();
            }
        }
    }
}
