using Core;
using Player;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// A single pooled projectile fired by a ranged enemy. Travels in a
    /// straight line and emits EnemyAttackEvent on hitting the player's
    /// collider, then returns itself to the pool. Also returns itself after
    /// lifetimeSeconds if it never hits anything.
    ///
    /// Movement is driven via Rigidbody.linearVelocity (not transform.position)
    /// so Unity's Continuous Dynamic CCD actually applies — a direct transform
    /// move bypasses CCD even on a Continuous Dynamic rigidbody, which caused
    /// the old Update()-based movement to tunnel through the player at speed 12.
    ///
    /// Lifetime ticks in FixedUpdate to stay on the same cadence as OnTriggerEnter.
    ///
    /// Prefab requirements:
    ///   - Collider (IsTrigger = true)
    ///   - Rigidbody: useGravity = false, isKinematic = false,
    ///                Collision Detection = Continuous Dynamic,
    ///                all rotation axes frozen
    ///
    /// Attach to: the projectile prefab root.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        [System.NonSerialized] public EnemyProjectilePool Pool;

        private Rigidbody _rb;
        private float _damage;
        private float _remainingLifetime;
        private bool _isActive;
        // Per-instance hit flag — design doc Section 3.5: a single projectile
        // can only register one hit on the player per launch.
        private bool _hasHit;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning("EnemyProjectile: collider should be set to IsTrigger.", this);

            _rb = GetComponent<Rigidbody>();
            if (_rb)
            {
                _rb.useGravity = false;
                _rb.isKinematic = false;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                Debug.LogWarning("EnemyProjectile: Rigidbody missing. Add one with useGravity=false, " +
                                 "isKinematic=false, Collision Detection=Continuous Dynamic.", this);
            }
        }

        /// <summary>Called by EnemyProjectilePool.Fire() to launch this projectile.</summary>
        public void Launch(Vector3 origin, Vector3 direction, float speed, float damage, float lifetimeSeconds)
        {
            transform.position = origin;
            if (_rb) _rb.position = origin;

            transform.rotation = Quaternion.LookRotation(direction);

            _damage = damage;
            _remainingLifetime = lifetimeSeconds;
            _isActive = true;
            _hasHit = false;

            // Set velocity so CCD is respected — CCD only applies to Rigidbody
            // velocity-driven movement, not to direct transform position writes.
            if (_rb)
                _rb.linearVelocity = direction.normalized * speed;
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f)
                ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _hasHit) return;
            if (!other.GetComponentInParent<PlayerController>()) return;

            _hasHit = true;
            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = _damage,
                Position = transform.position,
                IsRanged = true, // skip PlayerHealth proximity gate — collision IS the validation
            });

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _isActive = false;
            if (_rb) _rb.linearVelocity = Vector3.zero;
            Pool.Return(this);
        }
    }
}
