using UnityEngine;

namespace UI
{
    /// <summary>
    /// The one shape the choice screen understands. Stats, abilities, and
    /// any future choice type (melee enchants, etc.) all convert into this
    /// before reaching ChoiceScreenController — the screen itself never
    /// references StatModifier, AbilityDefinitionSo, or any other concrete
    /// type.
    /// </summary>
    public readonly struct ChoiceCardData
    {
        public string Title { get; }
        public string Description { get; }
        public Stats.Rarity Rarity { get; }
        public Sprite Icon { get; }

        /// <summary>Invoked when the player selects this card. Owns whatever actually applying the choice means.</summary>
        public System.Action OnSelected { get; }

        public ChoiceCardData(string title, string description, Stats.Rarity rarity, Sprite icon, System.Action onSelected)
        {
            Title = title;
            Description = description;
            Rarity = rarity;
            Icon = icon;
            OnSelected = onSelected;
        }
    }
}
