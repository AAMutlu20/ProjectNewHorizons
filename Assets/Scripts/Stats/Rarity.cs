namespace Stats
{
    /// <summary>
    /// Reward tier for stats and abilities. Ordered low to high so rarities
    /// can be compared directly (Epic > Rare evaluates true).
    /// Legendary is only ever offered as a boss-kill reward, never from a
    /// normal level-up — that gate lives in the level-up choice generator,
    /// not here.
    /// </summary>
    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
    }
}
