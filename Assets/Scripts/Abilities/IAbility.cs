using Stats;

namespace Abilities
{
    /// <summary>
    /// Runtime cast logic for one ability. PlayerAbilityManager owns the
    /// cooldown timer and calls Cast() when it elapses — implementations
    /// only need to know how to execute their effect once, at the given
    /// rarity. Each ability (Shockwave, DarkShield, MeteorSlam, LaserBeam,
    /// PoisonAura, CenterOfTheUniverse, ConeOfFire) gets its own class.
    /// </summary>
    public interface IAbility
    {
        /// <summary>Executes the ability's effect once, at castOrigin, scaled by rarity and the player's StatSheet.</summary>
        void Cast(UnityEngine.Vector3 castOrigin, Rarity rarity, StatSheet statSheet);
    }
}
