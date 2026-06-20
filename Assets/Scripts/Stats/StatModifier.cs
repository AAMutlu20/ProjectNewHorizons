using System;

namespace Stats
{
    /// <summary>
    /// One rolled stat reward — the result of a level-up or boss-kill choice.
    /// Immutable once created; pass this to StatSheet.ApplyModifier to grant it.
    /// </summary>
    [Serializable]
    public readonly struct StatModifier
    {
        public StatDefinitionSo Definition { get; }
        public Rarity Rarity { get; }
        public float Value { get; }

        public StatModifier(StatDefinitionSo definition, Rarity rarity)
        {
            Definition = definition;
            Rarity = rarity;
            Value = definition.GetValueForRarity(rarity);
        }
    }
}
