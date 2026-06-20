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
    /// Attach to: the projectile prefab root, with a Collider set to IsTrigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour
    {
        [System.NonSerialized] public EnemyProjectilePool Pool;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _remainingLifetime;
        private bool _isActive;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning("EnemyProjectile: collider should be set to IsTrigger.", this);
        }

        /// <summary>Called by EnemyProjectilePool.Fire() to launch this projectile.</summary>
        public void Launch(Vector3 origin, Vector3 direction, float speed, float damage, float lifetimeSeconds)
        {
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);

            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _remainingLifetime = lifetimeSeconds;
            _isActive = true;
        }

        private void Update()
        {
            if (!_isActive) return;

            transform.position += _direction * (_speed * Time.deltaTime);

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
                ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            if (!other.GetComponentInParent<PlayerController>()) return;

            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = _damage,
                Position = transform.position,
            });

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _isActive = false;
            Pool.Return(this);
        }
    }
}
