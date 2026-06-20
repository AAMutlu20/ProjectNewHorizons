using System;

namespace Stats
{
    /// <summary>
    /// One offered choice in a level-up or boss-reward selection — a stat
    /// rolled at a specific rarity, not yet applied. UI reads this to render
    /// a card; selecting it hands the wrapped StatModifier to StatSheet.
    /// </summary>
    [Serializable]
    public readonly struct StatChoiceOption
    {
        public StatModifier Modifier { get; }

        public StatChoiceOption(StatModifier modifier)
        {
            Modifier = modifier;
        }
    }
}
