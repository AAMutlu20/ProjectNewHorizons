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

        void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();

            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Logic owns position AND rotation via EnemyData/MoveRotation — physics
                // should never be allowed to touch either. Constraints alone weren't
                // enough: rotation constraints are enforced relative to the rigidbody's
                // inertia space, and collision response is a documented case where a
                // constrained axis can still pick up rotation from the solver. Once that
                // happens, our own MoveRotation calls are composed on top of an already-
                // tilted base orientation each frame, which reads as "rotates strangely"
                // and (since a tipped capsule's long axis is no longer vertical) as a
                // stretched/sunk-into-the-ground look even though no scale ever changed.
                // Making the body kinematic removes physics as a rotation/position source
                // entirely — MovePosition/MoveRotation still work and still generate
                // collision/trigger callbacks, but nothing but our own code can move it.
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        // Called by EnemyPool

        /// <summary>Initialise this view with fresh data. Called immediately after Get() from pool.</summary>
        public void Init(EnemyTypeSo typeSo, DifficultyParams diff, Vector3 worldPos,
            Transform playerTransform, SpatialGrid grid, System.Collections.Generic.List<EnemyView> activeList)
        {
            _data = EnemyData.Create(typeSo, diff, worldPos);

            // IMPORTANT: with RigidbodyInterpolation.Interpolate, Unity keeps an internal
            // "previous position" buffer separate from rb.position, used to lerp the
            // rendered mesh between physics steps. Writing rb.position only updates the
            // "current" side of that pair — the stale "previous" side (wherever this body
            // was the last time it was active, possibly clear across the map) is untouched.
            // The very next rendered frame then interpolates from that stale previous
            // position to the new spawn position, which is the long smear/sliver.
            // Toggling interpolation off and back on forces Unity to discard the stale
            // buffer and reseed both sides of it at the current position, so there's
            // nothing left for the interpolator to lerp from.
            transform.position = worldPos;
            transform.rotation = Quaternion.identity;

            if (rb != null)
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

            if (animator != null) animator.SetInteger(AnimState, (int)EnemyState.Spawning);
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

            // Tick the behaviour (writes to _data)
            _behaviour.Tick(dt);

            // Sync transform from data position via Rigidbody — keeps the physics
            // engine's internal state consistent with logic-driven movement,
            // which matters for a non-kinematic body that other objects can collide with.
            if (rb != null)
                rb.MovePosition(_data.Position);
            else
                transform.position = _data.Position;

            // Face movement direction (3D capsule rotates on Y axis instead of sprite-flipping).
            // IMPORTANT: when rb != null, rotation must go through rb.MoveRotation, not
            // transform.rotation directly. Mixing a physics-driven MovePosition with a
            // direct transform.rotation write on the same Rigidbody causes the visual
            // transform to be composed from two different update timelines (physics
            // interpolation vs. immediate write) — this is what produced the stretched/
            // sheared capsule look during Play, even though the prefab's rest pose was clean.
            var flatVel = new Vector3(_data.Velocity.x, 0f, _data.Velocity.z);
            if (flatVel.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(flatVel);
                if (rb != null)
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

            _data.Hp -= amount;

            if (knockbackForce > 0f)
            {
                var dir = _data.Position - sourcePosition;
                _behaviour.ApplyKnockback(dir, knockbackForce, knockbackDuration);
            }

            if (!(_data.Hp <= 0f)) return;
            _data.Hp = 0f;
            _behaviour.OnDeath();
            EventBus.Emit(new EnemyDiedEvent { Type = _data.Type, Position = _data.Position });
        }
    }
}