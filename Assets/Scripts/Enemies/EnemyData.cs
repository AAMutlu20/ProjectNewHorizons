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

        public Vector2 Position; // world position — EnemyView syncs transform from this
        public Vector2 Velocity; // set by BehaviourController each frame

        public float AttackTimer; // countdown to next attack
        public float SpawnTimer; // countdown out of Spawning state

        /// <summary>
        /// Initialise from a type SO + difficulty multipliers. Call this in EnemyPool.Get().
        /// </summary>
        public static EnemyData Create(EnemyTypeSo so, DifficultyParams diff, Vector2 worldPos)
        {
            return new EnemyData
            {
                Type = so.type,
                TypeSo = so,
                State = EnemyState.Spawning,
                Hp = so.baseHp * diff.HpMult,
                MaxHp = so.baseHp * diff.HpMult,
                Speed = so.baseSpeed  * diff.SpeedMult,
                Damage = so.baseDamage * diff.DamageMult,
                Position = worldPos,
                Velocity = Vector2.zero,
                AttackTimer = so.attackCooldown,
                SpawnTimer = 0.5f,// 0.5s spawn grace period
            };
        }

        public bool IsAlive => State != EnemyState.Dying && State != EnemyState.Inactive;
        public bool CanAttack  => State == EnemyState.Attacking && AttackTimer <= 0f;
        public bool IsSpawning => State == EnemyState.Spawning;
    }
}