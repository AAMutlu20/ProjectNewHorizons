namespace Stats
{
    /// <summary>
    /// How a stat's value combines with the base value it modifies.
    /// Flat adds a constant (e.g. +10 Attack Damage).
    /// PercentAdditive adds a percentage of the base value, and stacks
    /// additively with other percent bonuses of the same stat
    /// (e.g. +6% and +8% Ability Power together give +14%, not +14.48%).
    /// </summary>
    public enum StatModifierType
    {
        Flat,
        PercentAdditive,
    }
}
