namespace Abilities
{
    /// <summary>
    /// Every ability's per-rarity stat block must at minimum expose its own
    /// cooldown — that's the one field every ability shares and the only one
    /// AbilityRuntime needs to drive cast timing generically. Everything else
    /// (damage, radius, layers, beam count, etc.) is specific to each ability
    /// and lives on that ability's own stat struct, read directly by that
    /// ability's IAbility implementation instead of through this interface.
    /// </summary>
    public interface IAbilityRarityStats
    {
        float Cooldown { get; }
    }
}
