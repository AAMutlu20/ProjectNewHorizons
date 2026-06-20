using Difficulty;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// State machine states for an enemy.
    /// BehaviourController reads this; EnemyView drives the Animator from it.
    /// </summary>
    public enum EnemyState
    {
        Spawning,   // brief invincibility window while appearing
        Moving,     // heading toward the player
        Attacking,  // in attack range, playing attack animation
        Dying,      // death animation playing, not yet returned to pool
        Inactive,   // in pool, disabled
    }

    /// <summary>
    /// All runtime data for one enemy instance.
    /// Plain struct — lives on EnemyView, written by BehaviourController, read by EnemyView.
    /// Zero allocation: no classes, no lists, no references except the EnemyTypeSO.
    /// </summary>
    public struct EnemyData
    {
        // Per the design doc: a miniboss spawn is 1.5x stronger than the wave's
        // normal enemies. Applied once at spawn, on top of DifficultyParams.
        private const float MinibossStatMultiplier = 1.5f;
        private const float MinibossXpMultiplier = 1.5f;

        public EnemyType Type;
        public EnemyTypeSo TypeSo; // SO reference for stats — read-only at runtime
        public EnemyState State;

        // Which specific attack is currently playing, set by the active
        // IEnemyAttackBehaviour before entering EnemyState.Attacking. Archetypes
        // with only one attack can leave this at its default; Boss sets it
        // per-attack so its Animator Controller can branch to the matching clip.
        public int AttackId;

        public float Hp;
        public float MaxHp;
        public float XpValue; // already scaled by miniboss multiplier if applicable

        // Backing fields for Speed/Damage — read through the properties below,
        // which apply BuffMultiplier transparently. Callers that already read
        // enemy.Speed / enemy.Damage don't need to change.
        private float _baseSpeed;
        private float _baseDamage;

        public Vector3 Position; // world position (XZ plane, Y is ground height) — EnemyView syncs transform from this
        public Vector3 Velocity; // set by BehaviourController each frame, Y always 0

        public Vector3 Knockback;      // current knockback velocity, decays over KnockbackTimer
        public float   KnockbackTimer; // seconds remaining of knockback override

        // Stun suspends AI movement and attacking entirely, like knockback
        // does, but applies no displacement — used by abilities like
        // Shockwave that stun without pushing the enemy anywhere.
        public float StunTimer;

        public float AttackTimer; // countdown to next attack
        public float SpawnTimer; // countdown out of Spawning state

        // Set once at spawn by whichever system decided this spawn should be a
        // miniboss (see the wave system's miniboss-frequency rules). Read by
        // EnemyView to flag the death event and trigger the buffed/glow VFX.
        public bool IsMiniboss;

        // Temporary stat boost from a buff source (e.g. EyeWinged's buff pulse).
        // Multiplies Speed and Damage while active; decays to 1x when the
        // timer runs out. Distinct from IsMiniboss, which is permanent for
        // the enemy's lifetime — this is a renewable, timed effect.
        public float BuffMultiplier;
        public float BuffTimer;

        // Rolled once at spawn, then held constant — gives same-type enemies a bit of
        // individual variation instead of moving in perfect lockstep as a single mass.
        // Re-rolling these per frame would look like jitter; rolling once gives each
        // enemy a stable "personality" for its lifetime.
        public float SeekAngleJitter; // radians, added to the raw seek-toward-player angle

        public float Speed => _baseSpeed * BuffMultiplier;
        public float Damage => _baseDamage * BuffMultiplier;

        /// <summary>
        /// Initialise from a type SO + difficulty multipliers. Call this in EnemyPool.Get().
        /// isMiniboss is decided by the caller (the wave/spawn system), not rolled here —
        /// this method stays a pure function of its inputs.
        /// </summary>
        public static EnemyData Create(EnemyTypeSo so, DifficultyParams diff, Vector3 worldPos, bool isMiniboss = false)
        {
            // ±10% speed variation, rolled once per spawn — keeps same-type packs from
            // moving as one perfectly synced block while staying close to designed pace.
            var speedJitter = Random.Range(0.9f, 1.1f);

            var minibossMultiplier = isMiniboss ? MinibossStatMultiplier : 1f;
            var maxHp = so.baseHp * diff.HpMult * minibossMultiplier;

            return new EnemyData
            {
                Type = so.type,
                TypeSo = so,
                State = EnemyState.Spawning,
                AttackId = 0,
                Hp = maxHp,
                MaxHp = maxHp,
                _baseSpeed = so.baseSpeed * speedJitter,
                _baseDamage = so.baseDamage * diff.DamageMult * minibossMultiplier,
                XpValue = isMiniboss ? so.xpValue * MinibossXpMultiplier : so.xpValue,
                Position = worldPos,
                Velocity = Vector3.zero,
                Knockback = Vector3.zero,
                KnockbackTimer = 0f,
                StunTimer = 0f,
                AttackTimer = so.attackCooldown,
                SpawnTimer = 0.5f,// 0.5s spawn grace period
                IsMiniboss = isMiniboss,
                BuffMultiplier = 1f,
                BuffTimer = 0f,
                // ±12° (≈0.21 rad) — enough to break up a "wall of arrows" funnel effect
                // without enemies visibly missing the player or looking uncoordinated.
                SeekAngleJitter = Random.Range(-0.21f, 0.21f),
            };
        }

        public bool IsAlive => State != EnemyState.Dying && State != EnemyState.Inactive;
        public bool CanAttack  => State == EnemyState.Attacking && AttackTimer <= 0f;
        public bool IsSpawning => State == EnemyState.Spawning;
        public bool IsKnockedBack => KnockbackTimer > 0f;
        public bool IsStunned => StunTimer > 0f;
        public bool IsBuffed => BuffTimer > 0f;

        /// <summary>Applies or refreshes a timed buff. Called by buff-source abilities (e.g. EyeWinged's pulse).</summary>
        public void ApplyBuff(float multiplier, float durationSeconds)
        {
            BuffMultiplier = multiplier;
            BuffTimer = durationSeconds;
        }

        /// <summary>Applies or refreshes a stun. Called by stun-source abilities (e.g. Shockwave).</summary>
        public void ApplyStun(float durationSeconds)
        {
            if (durationSeconds > StunTimer)
                StunTimer = durationSeconds;
        }

        /// <summary>Call once per tick from BehaviourController to decay an active buff.</summary>
        public void TickBuff(float deltaTime)
        {
            if (BuffTimer <= 0f) return;

            BuffTimer -= deltaTime;
            if (BuffTimer <= 0f)
                BuffMultiplier = 1f;
        }
    }
}
