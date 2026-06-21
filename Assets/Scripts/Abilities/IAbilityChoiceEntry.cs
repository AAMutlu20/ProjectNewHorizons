using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// The common shape every grantable ability converts into, so the choice
    /// generator and screen never need to know whether an ability is
    /// cooldown-cast (Shockwave, via PlayerAbilityManager) or persistent
    /// (Dark Shield, Poison Aura, Center of the Universe, each with their
    /// own GrantXxx method and stat shape). One small wrapper class per
    /// ability implements this; adding a 9th ability later means writing
    /// one more wrapper, not touching the generator or screen.
    /// </summary>
    public interface IAbilityChoiceEntry
    {
        string DisplayName { get; }
        Sprite Icon { get; }

        /// <summary>True if this ability can be offered at the given rarity (e.g. Center of the Universe is Epic/Legendary only).</summary>
        bool IsAvailableAtRarity(Rarity rarity);

        /// <summary>A short description of what this ability does at this rarity, for the choice card. Numbers only -- no flavour text, since none is authored yet.</summary>
        string GetDescription(Rarity rarity);

        /// <summary>Actually grants the ability to the player at the given rarity.</summary>
        void Grant(Rarity rarity);
    }
}
