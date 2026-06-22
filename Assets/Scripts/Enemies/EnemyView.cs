using Core;
using Difficulty;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// The only MonoBehaviour on an enemy prefab.
    /// Owns the EnemyData struct and syncs it to the Transform and Animator.
    /// Receives damage via TakeDamage() — called by player attack scripts.
    ///
    /// Attach to: root of every enemy prefab.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] private Animator animator; // graybox enemies have none
        [SerializeField] private Rigidbody rb;      // kinematic — logic owns position/rotation, physics only for collision/trigger events

        // The actual runtime data — ref-accessible so BehaviourController writes directly
        private EnemyData _data;

        // Public read access for SpatialGrid and other systems
        public EnemyData Data => _data;
        public EnemyType Type => _data.Type;

        // Ref access for BehaviourController — avoids copy-on-read overhead
        public ref EnemyData DataRef => ref _data;

        // Set by EnemyPool after Get() — the pool this view belongs to
        [System.NonSerialized] public EnemyPool Pool;

        private BehaviourController _behaviour;
        private EnemyState _lastState;

        // Animator parameter hashes — cache them, string lookup is slow
        private static readonly int AnimState = Animator.StringToHash("State");
        private static readonly int AnimDead = Animator.StringToHash("Dead");

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();

            if (!rb) rb = GetComponent<Rigidbody>();
            if (!rb) return;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Called by EnemyPool

        /// <summary>Initialise this view with fresh data. Called immediately after Get() from pool.</summary>
        public void Init(EnemyTypeSo typeSo, DifficultyParams diff, Vector3 worldPos,
            Transform playerTransform, SpatialGrid grid, System.Collections.Generic.List<EnemyView> activeList,
            bool isMiniboss = false, EnemyProjectilePool projectilePool = null, VFX.AoeTelegraphRingPool telegraphPool = null)
        {
            _data = EnemyData.Create(typeSo, diff, worldPos, isMiniboss);
            
            transform.position = worldPos;
            transform.rotation = Quaternion.identity;

            if (rb)
            {
                rb.interpolation = RigidbodyInterpolation.None;

                rb.position = worldPos;
                rb.rotation = Quaternion.identity;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Wire BehaviourController references
            _behaviour.PlayerTransform = playerTransform;
            _behaviour.Grid = grid;
            _behaviour.ActiveEnemies = activeList;
            _behaviour.ProjectilePool = projectilePool;
            _behaviour.TelegraphPool = telegraphPool;

            if (animator) animator.SetInteger(AnimState, (int)EnemyState.Spawning);
            _lastState = EnemyState.Spawning;

            if (_data.Type == EnemyType.Boss)
                EmitBossHealthChanged();
        }

        /// <summary>
        /// Called by EnemyPool.FixedUpdate once per physics tick instead of using Unity's
        /// per-enemy Update(). Must stay on the physics cadence — it calls
        /// Rigidbody.MovePosition/MoveRotation internally, which require FixedUpdate timing.
        /// </summary>
        public void ManagedUpdate(float dt)
        {
            if (_data.State == EnemyState.Inactive) return;

            // Tick the behaviour (writes to _data)
            _behaviour.Tick(dt);
            
            if (rb)
                rb.MovePosition(_data.Position);
            else
                transform.position = _data.Position;
            
            var flatVel = new Vector3(_data.Velocity.x, 0f, _data.Velocity.z);
            if (flatVel.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(flatVel);
                if (rb)
                    rb.MoveRotation(targetRotation);
                else
                    transform.rotation = targetRotation;
            }

            // Drive Animator only when state changes — avoids SetInteger every frame
            if (animator && _data.State != _lastState)
            {
                animator.SetInteger(AnimState, (int)_data.State);
                if (_data.State == EnemyState.Dying)
                    animator.SetTrigger(AnimDead);
                _lastState = _data.State;
            }

            // Return to pool after death animation finishes
            // For graybox: return immediately on death. With animator: check normalizedTime.
            if (_data.State != EnemyState.Dying) return;
            var animDone = !animator ||
                           animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
            if (animDone)
                Pool.Return(this);
        }

        // Damage

        /// <summary>
        /// Called by player attack / weapon scripts on collision.
        /// sourcePosition + knockbackForce/Duration are optional — pass force = 0
        /// (the default) for damage with no push.
        /// </summary>
        public void TakeDamage(float amount, Vector3 sourcePosition = default,
            float knockbackForce = 0f, float knockbackDuration = 0.2f)
        {
            if (!_data.IsAlive) return;

            // Weaken applies here, at the single chokepoint every damage source
            // already passes through — melee, abilities, and any future source
            // all get the multiplier applied identically, with no per-source
            // special-casing needed.
            var weakenedAmount = amount * _data.WeakenMultiplier;
            _data.Hp -= weakenedAmount;

            if (knockbackForce > 0f)
            {
                var dir = _data.Position - sourcePosition;
                _behaviour.ApplyKnockback(dir, knockbackForce, knockbackDuration);
            }

            // Only the Boss has a persistent HUD health bar — other enemy types
            // don't need a per-hit event, so this stays Boss-specific rather
            // than firing for every enemy in the game.
            if (_data.Type == EnemyType.Boss)
                EmitBossHealthChanged();

            if (!(_data.Hp <= 0f)) return;
            _data.Hp = 0f;
            _behaviour.OnDeath();
            EventBus.Emit(new EnemyDiedEvent
            {
                Type = _data.Type,
                Position = _data.Position,
                XpValue = _data.XpValue,
                IsMiniboss = _data.IsMiniboss,
                IsBoss = _data.Type == EnemyType.Boss,
            });
        }

        private void EmitBossHealthChanged()
        {
            EventBus.Emit(new BossHealthChangedEvent
            {
                Current = _data.Hp,
                Max = _data.MaxHp,
            });
        }
    }

    /// <summary>Emitted whenever the Boss takes damage or spawns — drives the boss bar UI.</summary>
    public struct BossHealthChangedEvent
    {
        public float Current;
        public float Max;
    }
}
