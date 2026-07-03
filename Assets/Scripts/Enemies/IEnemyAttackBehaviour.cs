namespace Enemies
{
    /// <summary>
    /// Archetype-specific attack logic, called by BehaviourController once an
    /// enemy is within attackRange of the player. Each archetype (Zombie,
    /// Spider, EyeWinged, TornadoGhost, Boss) implements this as its own
    /// component attached alongside BehaviourController on that type's prefab.
    ///
    /// Movement, separation, knockback, and the spawn/dying state machine stay
    /// in BehaviourController for every archetype — only what happens at
    /// attack range differs.
    /// </summary>
    public interface IEnemyAttackBehaviour
    {
        /// <summary>
        /// Called every tick while the enemy is within attack range. Implementations
        /// own enemy.AttackTimer and enemy.State for the duration of the attack —
        /// BehaviourController hands off control rather than running its own logic.
        /// </summary>
        void TickAttack(ref EnemyData enemy, float deltaTime);

        /// <summary>
        /// Called by EnemyAnimationEventReceiver when an Animation Event fires
        /// on the current attack clip (e.g. "Release" on the windup's swing
        /// frame). eventName matches whatever string the animation clip's
        /// event was authored with in Unity's clip editor. Archetypes that
        /// don't use animation-driven timing (graybox capsules, no Animator)
        /// can leave this as a no-op via the default implementation — nothing
        /// breaks if no Animator ever calls it.
        /// </summary>
        void OnAttackAnimationEvent(string eventName) { }
    }

    /// <summary>
    /// Ambient ability that runs every tick regardless of distance to the
    /// player — e.g. Spider summoning minions, Eye buffing nearby enemies,
    /// Boss checking its miniboss-escort count. Kept separate from
    /// IEnemyAttackBehaviour so these don't get gated by attackRange.
    ///
    /// BehaviourController calls this through EnemyPool's existing managed
    /// update loop — implementations must NOT add their own Update(), or
    /// they reintroduce the per-enemy MonoBehaviour.Update() cost the pool
    /// was built to avoid.
    /// </summary>
    public interface IPeriodicAbility
    {
        void TickAbility(ref EnemyData enemy, float deltaTime);
    }

    /// <summary>
    /// Emitted by an IEnemyAttackBehaviour when an attack lands on the player.
    /// SlowFraction/SlowDuration default to zero — only attacks that actually
    /// apply a slow (e.g. Spider webs) need to set them.
    /// </summary>
    public struct EnemyAttackEvent
    {
        public float Damage;
        public UnityEngine.Vector3 Position;
        public float SlowFraction;
        public float SlowDuration;

        /// <summary>
        /// Set to true when the caller has already confirmed the hit geometrically
        /// (e.g. a raycast) and PlayerHealth should skip its proximity distance check.
        /// False for melee/contact attacks that report the enemy's position and
        /// rely on the distance check to confirm they're actually adjacent.
        /// </summary>
        public bool HitConfirmed;
    }
}
