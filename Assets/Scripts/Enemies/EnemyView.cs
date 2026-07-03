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
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] private Animator animator; // graybox enemies have none

        [SerializeField]
        private Rigidbody rb; // kinematic — logic owns position/rotation, physics only for collision/trigger events

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

            // Non-kinematic with gravity enabled, but Y is locked via
            // FreezePositionY below -- see that constraint's comment for why.
            // useGravity stays true (harmless with Y frozen) since
            // SpawnPositionResolver's raycast still needs real downward physics
            // queries to work for spawn placement.
            rb.isKinematic = false;
            rb.useGravity = true;

            // FreezePositionY: enemies were randomly falling through the floor
            // over time, regardless of collider setup -- root cause not fully
            // diagnosed, but locking Y via this hard constraint stops it outright.
            // Tradeoff accepted: ground is treated as flat going forward -- no
            // falling, no climbing onto raised platforms via physics anymore.
            rb.constraints = RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ
                             | RigidbodyConstraints.FreezePositionY;

            // Continuous detection avoids tunnelling through thin platform edges at
            // normal enemy speeds.
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Called by EnemyPool

        /// <summary>Initialise this view with fresh data. Called immediately after Get() from pool.</summary>
        public void Init(EnemyTypeSo typeSo, DifficultyParams diff, Vector3 worldPos,
            Transform playerTransform, SpatialGrid grid, System.Collections.Generic.List<EnemyView> activeList,
            bool isMiniboss = false,
            VFX.AoeTelegraphRingPool telegraphPool = null,
            LayerMask obstructionLayers = default, Stats.StatSheet playerStatSheet = null)
        {
            // worldPos arrives here ALREADY fully resolved -- EnemyPool.Get()
            // calls SpawnPositionResolver first, which raycasts to find the
            // REAL ground height at this X/Z and adds typeSo.groundOffsetY on
            // top of that actual detected surface.
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
            _behaviour.TelegraphPool = telegraphPool;
            _behaviour.ObstructionLayers = obstructionLayers;
            _behaviour.PlayerStatSheet = playerStatSheet;

            // Re-arms the Zombie's trigger-based explosion on every reuse from
            // the pool (clears the one-shot _hasExploded flag from any
            // previous life). GetComponentInChildren returns null gracefully
            // for every other archetype that doesn't have this child at all --
            // safe to call unconditionally here rather than special-casing by type.
            var explodeBehaviour = GetComponentInChildren<ZombieExplodeBehaviour>(includeInactive: true);
            if (explodeBehaviour) explodeBehaviour.ResetForReuse(this);

            if (animator) animator.SetInteger(AnimState, (int)EnemyState.Spawning);
            _lastState = EnemyState.Spawning;

        }

        /// <summary>
        /// Called by EnemyPool.FixedUpdate once per physics tick instead of using Unity's
        /// per-enemy Update(). Must stay on the physics cadence — it calls
        /// Rigidbody.MovePosition/MoveRotation internally, which require FixedUpdate timing.
        /// </summary>
        public void ManagedUpdate(float dt)
        {
            if (_data.State == EnemyState.Inactive) return;

            // Tick the behaviour (writes to _data.Velocity, X/Z only -- Y is
            // always zeroed by BehaviourController's own movement/knockback math)
            _behaviour.Tick(dt);

            if (rb)
            {
                // Apply ONLY the horizontal intent as a delta on top of the
                // rigidbody's current position -- Y is locked by
                // FreezePositionY (see Awake), so this never touches it either
                // way, but keeping movement delta-based (not an absolute
                // position snap) avoids fighting the constraint or physics
                // solver in general.
                var horizontalDelta = new Vector3(_data.Velocity.x, 0f, _data.Velocity.z) * dt;
                rb.MovePosition(rb.position + horizontalDelta);

                // MovePosition on a non-kinematic body fights with the solver's own
                // resting-contact response -- gravity keeps integrating into
                // linearVelocity.y every step with nothing capping it back down the
                // way a normal (non-MovePosition-driven) resting body would self-correct.
                // Left unchecked, that buildup eventually destabilizes the solver enough
                // to tunnel through the floor even with Continuous Dynamic CCD. Clamping
                // here keeps a resting enemy's fall speed from ever exceeding one frame's
                // worth of gravity, while still allowing it to fall normally off a ledge.
                var clampedFallSpeed = Mathf.Max(rb.linearVelocity.y, -Mathf.Abs(Physics.gravity.y) * dt * 2f);
                rb.linearVelocity = new Vector3(0f, clampedFallSpeed, 0f);

                // Sync _data.Position back from the REAL post-physics rigidbody
                // position -- every other system (abilities, attack-range checks)
                // reads enemy.Position expecting it to be ground truth.
                _data.Position = rb.position;
            }
            else
            {
                transform.position = _data.Position;
            }

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
            var weakenedAmount = amount * _data.GetWeakenDamageMultiplier();
            _data.Hp -= weakenedAmount;

            if (knockbackForce > 0f)
            {
                var dir = _data.Position - sourcePosition;
                _behaviour.ApplyKnockback(dir, knockbackForce, knockbackDuration);
            }

            // Only the Boss has a persistent HUD health bar — other enemy types
            // don't need a per-hit event, so this stays Boss-specific rather
            // than firing for every enemy in the game.

            if (!(_data.Hp <= 0f)) return;
            _data.Hp = 0f;
            _behaviour.OnDeath();
            EventBus.Emit(new EnemyDiedEvent
            {
                Type = _data.Type,
                Position = _data.Position,
                XpValue = _data.XpValue,
                IsMiniboss = _data.IsMiniboss,
            });
        }
    }
}
