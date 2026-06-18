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
        public EnemyType Type;
        public EnemyTypeSo TypeSo; // SO reference for stats — read-only at runtime
        public EnemyState State;

        public float Hp;
        public float MaxHp;
        public float Speed; // already multiplied by DifficultyParams.speedMult
        public float Damage; // already multiplied by DifficultyParams.damageMult

        public Vector3 Position; // world position (XZ plane, Y is ground height) — EnemyView syncs transform from this
        public Vector3 Velocity; // set by BehaviourController each frame, Y always 0

        public Vector3 Knockback;      // current knockback velocity, decays over KnockbackTimer
        public float   KnockbackTimer; // seconds remaining of knockback override

        public float AttackTimer; // countdown to next attack
        public float SpawnTimer; // countdown out of Spawning state

        // Rolled once at spawn, then held constant — gives same-type enemies a bit of
        // individual variation instead of moving in perfect lockstep as a single mass.
        // Re-rolling these per frame would look like jitter; rolling once gives each
        // enemy a stable "personality" for its lifetime.
        public float SeekAngleJitter; // radians, added to the raw seek-toward-player angle

        /// <summary>
        /// Initialise from a type SO + difficulty multipliers. Call this in EnemyPool.Get().
        /// </summary>
        public static EnemyData Create(EnemyTypeSo so, DifficultyParams diff, Vector3 worldPos)
        {
            // ±10% speed variation, rolled once per spawn — keeps same-type packs from
            // moving as one perfectly synced block while staying close to designed pace.
            var speedJitter = Random.Range(0.9f, 1.1f);

            return new EnemyData
            {
                Type = so.type,
                TypeSo = so,
                State = EnemyState.Spawning,
                Hp = so.baseHp * diff.HpMult,
                MaxHp = so.baseHp * diff.HpMult,
                Speed = so.baseSpeed * diff.SpeedMult * speedJitter,
                Damage = so.baseDamage * diff.DamageMult,
                Position = worldPos,
                Velocity = Vector3.zero,
                Knockback = Vector3.zero,
                KnockbackTimer = 0f,
                AttackTimer = so.attackCooldown,
                SpawnTimer = 0.5f,// 0.5s spawn grace period
                // ±12° (≈0.21 rad) — enough to break up a "wall of arrows" funnel effect
                // without enemies visibly missing the player or looking uncoordinated.
                SeekAngleJitter = Random.Range(-0.21f, 0.21f),
            };
        }

        public bool IsAlive => State != EnemyState.Dying && State != EnemyState.Inactive;
        public bool CanAttack  => State == EnemyState.Attacking && AttackTimer <= 0f;
        public bool IsSpawning => State == EnemyState.Spawning;
        public bool IsKnockedBack => KnockbackTimer > 0f;
    }
}