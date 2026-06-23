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
        private const float DotTickInterval = 1f; // matches the project's per-second DOT convention

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
        // which apply BuffMultiplier and the Slow stack transparently. Callers
        // that already read enemy.Speed / enemy.Damage don't need to change.
        private float _baseSpeed;
        private float _baseDamage;

        public Vector3 Position; // world position (XZ plane, Y is ground height) — EnemyView syncs transform from this
        public Vector3 Velocity; // set by BehaviourController each frame, Y always 0

        public Vector3 Knockback;      // current knockback velocity, decays over KnockbackTimer
        public float   KnockbackTimer; // seconds remaining of knockback override

        // Stun suspends AI movement and attacking entirely, like knockback
        // does, but applies no displacement — used by abilities like
        // Shockwave that stun without pushing the enemy anywhere. Explicitly
        // NOT stacking, per design -- duration only, take-the-longer rule.
        public float StunTimer;
        public float StunMaxDuration; // for an accurate UI radial countdown (StunTimer / StunMaxDuration) -- same reasoning as DebuffStack.MaxDuration

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
        // NOT a debuff (an enemy buffing another enemy is not the player
        // inflicting damage) -- does not use the DebuffStack model at all.
        public float BuffMultiplier;
        public float BuffTimer;

        // ---- Player-inflicted debuffs, all using the shared DebuffStack
        // stacking model: each new application adds an independent stack
        // (not overwrite/refresh), all stacks share one timer, stack
        // contribution diminishes per DebuffStack.GetTotalMultiplier
        // (1.0x / +0.5x / +0.25x / +0.10x each beyond that). No stack cap.

        // Weaken (e.g. Laser Beam): increases damage taken from EVERY source
        // -- applied at the single TakeDamage chokepoint in EnemyView.
        // WeakenBaseFraction is the fraction from the MOST RECENT application
        // (e.g. 0.10 for Laser Beam's base "10% more damage taken") -- the
        // diminishing stack multiplier is applied on top of that base. Only
        // one weaken source exists in the design today, so "most recent
        // base" is the simplest correct behaviour; if multiple differently-
        // scaled weaken sources exist later, this would need revisiting.
        public DebuffStack WeakenStack;
        public float WeakenBaseFraction;

        // Slow (e.g. Poison Aura's Legendary tier): reduces Speed.
        // SlowBaseFraction is the fraction from the most recent application
        // (e.g. 0.5 for "50% slower"), same "most recent base" caveat as Weaken.
        public DebuffStack SlowStack;
        public float SlowBaseFraction;

        // Bleed (e.g. Cleaving Attacks): percent of THIS enemy's own MaxHp
        // per second, matching the doc's "%maxhp/s". Each stack's damage is
        // rolled INDEPENDENTLY per tick against the attacker's current crit
        // chance/damage (see BehaviourController.TickBleed, which needs the
        // player's StatSheet to do this) -- not baked in once at application.
        public DebuffStack BleedStack;
        public float BleedBaseDamagePercentPerSecond;
        private float _bleedTickTimer;

        // Burn (Cone of Fire's residual fire): same mechanism as Bleed, but
        // FLAT damage per second rather than a percent of max HP.
        public DebuffStack BurnStack;
        public float BurnBaseDamagePerSecond;
        private float _burnTickTimer;

        // Rolled once at spawn, then held constant — gives same-type enemies a bit of
        // individual variation instead of moving in perfect lockstep as a single mass.
        // Re-rolling these per frame would look like jitter; rolling once gives each
        // enemy a stable "personality" for its lifetime.
        public float SeekAngleJitter; // radians, added to the raw seek-toward-player angle

        public float Speed => _baseSpeed * BuffMultiplier * GetSlowSpeedMultiplier();
        public float Damage => _baseDamage * BuffMultiplier;

        private float GetSlowSpeedMultiplier()
        {
            if (!SlowStack.IsActive) return 1f;
            var totalSlowFraction = SlowBaseFraction * SlowStack.GetTotalMultiplier();
            return Mathf.Max(0f, 1f - totalSlowFraction);
        }

        /// <summary>The current total "take X% more damage" multiplier from Weaken stacks, 1 (no effect) if not weakened.</summary>
        public float GetWeakenDamageMultiplier()
        {
            if (!WeakenStack.IsActive) return 1f;
            return 1f + WeakenBaseFraction * WeakenStack.GetTotalMultiplier();
        }

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
                StunMaxDuration = 0f,
                AttackTimer = so.attackCooldown,
                SpawnTimer = 0.5f,// 0.5s spawn grace period
                IsMiniboss = isMiniboss,
                BuffMultiplier = 1f,
                BuffTimer = 0f,
                WeakenStack = default,
                WeakenBaseFraction = 0f,
                SlowStack = default,
                SlowBaseFraction = 0f,
                BleedStack = default,
                BleedBaseDamagePercentPerSecond = 0f,
                _bleedTickTimer = 0f,
                BurnStack = default,
                BurnBaseDamagePerSecond = 0f,
                _burnTickTimer = 0f,
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
        public float StunRemainingFraction01 => StunMaxDuration > 0f ? Mathf.Clamp01(StunTimer / StunMaxDuration) : 0f;
        public bool IsBuffed => BuffTimer > 0f;
        public bool IsWeakened => WeakenStack.IsActive;
        public bool IsSlowed => SlowStack.IsActive;
        public bool IsBleeding => BleedStack.IsActive;
        public bool IsBurning => BurnStack.IsActive;

        /// <summary>Applies or refreshes a timed buff. Called by buff-source abilities (e.g. EyeWinged's pulse).</summary>
        public void ApplyBuff(float multiplier, float durationSeconds)
        {
            BuffMultiplier = multiplier;
            BuffTimer = durationSeconds;
        }

        /// <summary>Applies or refreshes a stun. NOT stacking, per design -- take the longer duration. Called by stun-source abilities (e.g. Shockwave).</summary>
        public void ApplyStun(float durationSeconds)
        {
            if (durationSeconds > StunTimer)
            {
                StunTimer = durationSeconds;
                StunMaxDuration = durationSeconds;
            }
        }

        /// <summary>
        /// Adds an independent Weaken stack (e.g. Laser Beam). baseFraction is
        /// this application's base "take X% more damage" value, used for ALL
        /// current stacks going forward (most-recent-base, see WeakenBaseFraction's
        /// doc comment) -- the diminishing stack multiplier is applied on top.
        /// </summary>
        public void ApplyWeaken(float baseFraction, float durationSeconds)
        {
            WeakenBaseFraction = baseFraction;
            WeakenStack.AddStack(durationSeconds);
        }

        /// <summary>Adds an independent Slow stack (e.g. Poison Aura's Legendary tier). Same stacking model as ApplyWeaken.</summary>
        public void ApplySlow(float slowFraction, float durationSeconds)
        {
            SlowBaseFraction = slowFraction;
            SlowStack.AddStack(durationSeconds);
        }

        /// <summary>Adds an independent Bleed stack (e.g. Cleaving Attacks). Same stacking model as ApplyWeaken.</summary>
        public void ApplyBleed(float damagePercentPerSecond, float durationSeconds)
        {
            BleedBaseDamagePercentPerSecond = damagePercentPerSecond;
            BleedStack.AddStack(durationSeconds);
        }

        /// <summary>Adds an independent Burn stack (Cone of Fire's residual fire). Same stacking model as ApplyWeaken.</summary>
        public void ApplyBurn(float damagePerSecond, float durationSeconds)
        {
            BurnBaseDamagePerSecond = damagePerSecond;
            BurnStack.AddStack(durationSeconds);
        }

        /// <summary>Call once per tick from BehaviourController to decay an active buff.</summary>
        public void TickBuff(float deltaTime)
        {
            if (BuffTimer <= 0f) return;

            BuffTimer -= deltaTime;
            if (BuffTimer <= 0f)
                BuffMultiplier = 1f;
        }

        /// <summary>Call once per tick from BehaviourController to decay the Weaken stack's shared timer.</summary>
        public void TickWeaken(float deltaTime) => WeakenStack.Tick(deltaTime);

        /// <summary>Call once per tick from BehaviourController to decay the Slow stack's shared timer.</summary>
        public void TickSlow(float deltaTime) => SlowStack.Tick(deltaTime);

        /// <summary>
        /// Decays the Bleed stack's shared timer and, once per DotTickInterval,
        /// rolls EACH active stack's damage independently against the
        /// attacker's crit chance/damage (per design: every stack's damage
        /// scales off attack damage/crit independently per tick, not once at
        /// application). Returns true exactly on the frame a damage tick
        /// should be applied -- the CALLER (BehaviourController) deals the
        /// actual damage via EnemyView.TakeDamage.
        /// </summary>
        public bool TickBleed(float deltaTime, System.Func<float> rollCritMultiplier, out float tickDamage)
        {
            tickDamage = 0f;
            BleedStack.Tick(deltaTime);
            if (!BleedStack.IsActive) return false;

            _bleedTickTimer -= deltaTime;
            if (_bleedTickTimer > 0f) return false;

            _bleedTickTimer = DotTickInterval;

            // Each stack rolls its own crit independently -- summing
            // pre-crit-multiplied per-stack damage, not applying one crit
            // roll to the combined total.
            for (var stackIndex = 1; stackIndex <= BleedStack.StackCount; stackIndex++)
            {
                var stackBaseDamage = MaxHp * BleedBaseDamagePercentPerSecond * DotTickInterval
                                       * GetStackContributionForIndex(stackIndex);
                tickDamage += stackBaseDamage * rollCritMultiplier();
            }

            return true;
        }

        /// <summary>Same as TickBleed, but FLAT damage per stack rather than a percent of MaxHp.</summary>
        public bool TickBurn(float deltaTime, System.Func<float> rollCritMultiplier, out float tickDamage)
        {
            tickDamage = 0f;
            BurnStack.Tick(deltaTime);
            if (!BurnStack.IsActive) return false;

            _burnTickTimer -= deltaTime;
            if (_burnTickTimer > 0f) return false;

            _burnTickTimer = DotTickInterval;

            for (var stackIndex = 1; stackIndex <= BurnStack.StackCount; stackIndex++)
            {
                var stackBaseDamage = BurnBaseDamagePerSecond * DotTickInterval
                                      * GetStackContributionForIndex(stackIndex);
                tickDamage += stackBaseDamage * rollCritMultiplier();
            }

            return true;
        }

        // Mirrors DebuffStack's private GetStackContribution -- duplicated
        // here (rather than exposed publicly on DebuffStack) since only
        // the per-stack DOT rolling above needs per-INDEX contribution;
        // everything else only ever needs the pre-summed GetTotalMultiplier.
        private static float GetStackContributionForIndex(int stackIndex)
        {
            switch (stackIndex)
            {
                case 1: return 1.0f;
                case 2: return 0.5f;
                case 3: return 0.25f;
                default: return 0.10f;
            }
        }
    }
}
