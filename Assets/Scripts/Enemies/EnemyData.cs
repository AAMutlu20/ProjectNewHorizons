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
        private const float BleedTickInterval = 1f; // matches the project's per-second DOT convention

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
        // which apply BuffMultiplier and SlowMultiplier transparently. Callers
        // that already read enemy.Speed / enemy.Damage don't need to change.
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

        // Weaken debuff (e.g. Laser Beam): increases damage taken from EVERY
        // source — melee, abilities, even other enemies' splash — since it's
        // applied at the single TakeDamage chokepoint in EnemyView rather
        // than at each individual damage source. Expressed as a multiplier
        // (1.10 = take 10% more damage), defaulting to 1 (no effect).
        public float WeakenMultiplier;
        public float WeakenTimer;

        // Slow debuff (e.g. Poison Aura's Legendary tier): reduces Speed by a
        // fraction. Expressed as a multiplier already inverted for direct use
        // (0.5 fraction slow -> SlowMultiplier = 0.5, so Speed is halved) —
        // unlike Weaken, only one source exists in the doc so far, so this
        // stays a single value rather than a stacked-source list like the
        // player's SlowEffectController.
        public float SlowMultiplier;
        public float SlowTimer;

        // Bleed DOT (e.g. Cleaving Attacks): unlike Weaken/Slow/Buff, this
        // ACTIVELY deals damage rather than just modifying a multiplier —
        // BehaviourController.TickBleed calls EnemyView.TakeDamage directly
        // when BleedTickTimer elapses. BleedDamagePerSecond is a percent of
        // THIS enemy's own MaxHp per second, matching the doc's "%maxhp/s".
        public float BleedDamagePercentPerSecond;
        public float BleedTimer;
        public float BleedTickTimer;

        // Burn DOT (Cone of Fire's residual fire): same mechanism as Bleed,
        // but FLAT damage per second rather than a percent of max HP — the
        // doc's Cone of Fire values (Damage: 20, ResidualDmg: 10) are clearly
        // flat numbers in the same scale as a normal hit, not percentages,
        // unlike Cleaving's bleed which is explicitly "%maxhp/s". Kept as its
        // own separate timer/field set rather than reusing Bleed, since the
        // two have genuinely different units and the doc never says a burning
        // enemy can't also be bleeding from something else at the same time.
        public float BurnDamagePerSecond;
        public float BurnTimer;
        public float BurnTickTimer;

        // Rolled once at spawn, then held constant — gives same-type enemies a bit of
        // individual variation instead of moving in perfect lockstep as a single mass.
        // Re-rolling these per frame would look like jitter; rolling once gives each
        // enemy a stable "personality" for its lifetime.
        public float SeekAngleJitter; // radians, added to the raw seek-toward-player angle

        public float Speed => _baseSpeed * BuffMultiplier * SlowMultiplier;
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
                WeakenMultiplier = 1f,
                WeakenTimer = 0f,
                SlowMultiplier = 1f,
                SlowTimer = 0f,
                BleedDamagePercentPerSecond = 0f,
                BleedTimer = 0f,
                BleedTickTimer = 0f,
                BurnDamagePerSecond = 0f,
                BurnTimer = 0f,
                BurnTickTimer = 0f,
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
        public bool IsWeakened => WeakenTimer > 0f;
        public bool IsSlowed => SlowTimer > 0f;
        public bool IsBleeding => BleedTimer > 0f;
        public bool IsBurning => BurnTimer > 0f;

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

        /// <summary>
        /// Applies or refreshes a weaken debuff (e.g. Laser Beam). If already
        /// weakened, takes the STRONGER of the two multipliers rather than
        /// just resetting the timer — being hit by two different weaken
        /// sources should apply the bigger one, not silently downgrade it.
        /// </summary>
        public void ApplyWeaken(float multiplier, float durationSeconds)
        {
            if (multiplier > WeakenMultiplier)
                WeakenMultiplier = multiplier;
            if (durationSeconds > WeakenTimer)
                WeakenTimer = durationSeconds;
        }

        /// <summary>
        /// Applies or refreshes a slow (e.g. Poison Aura's Legendary tier).
        /// slowMultiplier is the resulting Speed multiplier (0.5 = half speed),
        /// not the slow fraction — takes the STRONGER slow (lower multiplier)
        /// if already slowed, same "don't downgrade an existing effect" rule
        /// as ApplyWeaken.
        /// </summary>
        public void ApplySlow(float slowMultiplier, float durationSeconds)
        {
            if (slowMultiplier < SlowMultiplier)
                SlowMultiplier = slowMultiplier;
            if (durationSeconds > SlowTimer)
                SlowTimer = durationSeconds;
        }

        /// <summary>
        /// Applies or refreshes bleed (e.g. Cleaving Attacks). Takes the
        /// STRONGER percent-per-second rate if already bleeding, same
        /// "don't downgrade" rule as Weaken/Slow. Does not reset the tick
        /// timer, so re-applying bleed mid-tick doesn't delay the next tick.
        /// </summary>
        public void ApplyBleed(float damagePercentPerSecond, float durationSeconds)
        {
            if (damagePercentPerSecond > BleedDamagePercentPerSecond)
                BleedDamagePercentPerSecond = damagePercentPerSecond;
            if (durationSeconds > BleedTimer)
                BleedTimer = durationSeconds;
        }

        /// <summary>
        /// Applies or refreshes burn (Cone of Fire's residual fire). Same
        /// "don't downgrade, don't reset the tick timer" rules as ApplyBleed,
        /// but FLAT damage per second rather than a percent of max HP.
        /// </summary>
        public void ApplyBurn(float damagePerSecond, float durationSeconds)
        {
            if (damagePerSecond > BurnDamagePerSecond)
                BurnDamagePerSecond = damagePerSecond;
            if (durationSeconds > BurnTimer)
                BurnTimer = durationSeconds;
        }

        /// <summary>Call once per tick from BehaviourController to decay an active buff.</summary>
        public void TickBuff(float deltaTime)
        {
            if (BuffTimer <= 0f) return;

            BuffTimer -= deltaTime;
            if (BuffTimer <= 0f)
                BuffMultiplier = 1f;
        }

        /// <summary>Call once per tick from BehaviourController to decay an active weaken debuff.</summary>
        public void TickWeaken(float deltaTime)
        {
            if (WeakenTimer <= 0f) return;

            WeakenTimer -= deltaTime;
            if (WeakenTimer <= 0f)
                WeakenMultiplier = 1f;
        }

        /// <summary>Call once per tick from BehaviourController to decay an active slow.</summary>
        public void TickSlow(float deltaTime)
        {
            if (SlowTimer <= 0f) return;

            SlowTimer -= deltaTime;
            if (SlowTimer <= 0f)
                SlowMultiplier = 1f;
        }

        /// <summary>
        /// Decays the bleed duration and its internal tick timer. Returns
        /// true exactly on the frame a damage tick should be applied — the
        /// CALLER (BehaviourController) is responsible for actually dealing
        /// the damage via EnemyView.TakeDamage, since EnemyData itself has
        /// no reference back to EnemyView.
        /// </summary>
        public bool TickBleed(float deltaTime, out float tickDamage)
        {
            tickDamage = 0f;
            if (BleedTimer <= 0f) return false;

            BleedTimer -= deltaTime;
            if (BleedTimer <= 0f)
            {
                BleedDamagePercentPerSecond = 0f;
                return false;
            }

            BleedTickTimer -= deltaTime;
            if (BleedTickTimer > 0f) return false;

            BleedTickTimer = BleedTickInterval;
            tickDamage = MaxHp * BleedDamagePercentPerSecond * BleedTickInterval;
            return true;
        }

        /// <summary>
        /// Decays the burn duration and its internal tick timer. Same shape
        /// as TickBleed, but the resulting tickDamage is flat, not a percent
        /// of MaxHp -- the caller (BehaviourController) still owns actually
        /// dealing the damage via EnemyView.TakeDamage.
        /// </summary>
        public bool TickBurn(float deltaTime, out float tickDamage)
        {
            tickDamage = 0f;
            if (BurnTimer <= 0f) return false;

            BurnTimer -= deltaTime;
            if (BurnTimer <= 0f)
            {
                BurnDamagePerSecond = 0f;
                return false;
            }

            BurnTickTimer -= deltaTime;
            if (BurnTickTimer > 0f) return false;

            BurnTickTimer = BleedTickInterval; // same per-second tick cadence as bleed
            tickDamage = BurnDamagePerSecond * BleedTickInterval;
            return true;
        }
    }
}
