using Stats;

namespace Abilities
{
    /// <summary>One offered ability choice -- an entry rolled at a specific rarity, not yet granted.</summary>
    public readonly struct AbilityChoiceOption
    {
        public IAbilityChoiceEntry Entry { get; }
        public Rarity Rarity { get; }

        public AbilityChoiceOption(IAbilityChoiceEntry entry, Rarity rarity)
        {
            Entry = entry;
            Rarity = rarity;
        }
    }
}
