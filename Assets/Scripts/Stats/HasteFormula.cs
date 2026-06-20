namespace Stats
{
    /// <summary>
    /// Converts Ability Haste into casting rate and cooldown.
    ///
    /// Haste increases casting rate linearly: 1 point of haste = 1% faster
    /// casting. The resulting cooldown follows from inverting that rate, so
    /// cooldown decreases hyperbolically — 100 haste halves the cooldown,
    /// 200 haste reduces it to a third, and cooldown reduction approaches
    /// (but never reaches) 100% as haste grows without bound.
    /// </summary>
    public static class HasteFormula
    {
        private const float HasteToRatePercent = 100f;

        public static float GetCastingRate(float baseCastingRate, float haste)
        {
            return baseCastingRate * (1f + haste / HasteToRatePercent);
        }

        public static float GetCooldown(float baseCooldown, float haste)
        {
            return baseCooldown * (HasteToRatePercent / (HasteToRatePercent + haste));
        }

        public static float GetCooldownReductionPercent(float haste)
        {
            return haste / (HasteToRatePercent + haste) * HasteToRatePercent;
        }
    }
}
