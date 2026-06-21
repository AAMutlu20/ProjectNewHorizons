using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// A melee enchant modifies the player's melee swing — called once per
    /// landed hit, after damage has already been dealt. Each enchant
    /// (Cleaving Attacks, Lifesteal, Knockback) implements this as its own
    /// component attached alongside MeleeWeapon.
    ///
    /// Unlike IEnemyAttackBehaviour, multiple enchants can be active at once
    /// (the player can own all three) — MeleeWeapon calls every attached
    /// enchant for every landed hit, not just one.
    /// </summary>
    public interface IMeleeEnchant
    {
        /// <summary>
        /// Called once per enemy actually hit during a swing, after damage
        /// has been applied. damageDealt is the amount that specific hit did
        /// (post-crit), in case an enchant's effect scales off it (Lifesteal).
        /// </summary>
        void OnMeleeHit(EnemyView target, float damageDealt, Vector3 hitOrigin);

        /// <summary>
        /// Called once per swing, after all hits for that swing have landed —
        /// for enchants that care about "how many enemies this swing hit" or
        /// need to act once per swing rather than once per individual target
        /// (e.g. Cleaving's "every Nth attack" counter).
        /// </summary>
        void OnSwingComplete(int enemiesHitThisSwing) { }

        /// <summary>
        /// If this enchant defines a total knockback force for its tier (only
        /// the Knockback enchant does), returns it. Returns null for every
        /// other enchant, meaning "no opinion" — MeleeWeapon falls back to
        /// its own baseKnockbackForce if no enchant returns a value.
        /// </summary>
        float? GetKnockbackForceOverride() => null;
    }
}
