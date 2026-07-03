using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// A pooled projectile that travels in a straight line and damages the
    /// player on contact using the same trigger pattern as ZombieExplodeBehaviour
    /// — OnTriggerEnter checks for PlayerHealth in the parent chain, emits
    /// EnemyAttackEvent, then returns to pool.
    ///
    /// The old approach checked for PlayerController instead of PlayerHealth,
    /// which was inconsistent with everything else and caused misses. This
    /// matches the proven zombie pattern exactly.
    ///
    /// Attach to: the projectile prefab root. Collider must be IsTrigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask playerBodyLayers;

        [System.NonSerialized] public EnemyProjectilePool Pool;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _remainingLifetime;
        private bool _isActive;
        private bool _hasHit;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning("EnemyProjectile: collider should be set to IsTrigger.", this);
        }

        public void Launch(Vector3 origin, Vector3 direction, float speed, float damage, float lifetimeSeconds)
        {
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);

            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _remainingLifetime = lifetimeSeconds;
            _isActive = true;
            _hasHit = false;
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
            if (!_isActive || _hasHit) return;

            // Same layer + PlayerHealth check as ZombieExplodeBehaviour —
            // the proven pattern that actually works.
            if (playerBodyLayers != 0 && (playerBodyLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            var playerHealth = other.GetComponentInParent<Player.PlayerHealth>();
            if (!playerHealth) return;

            _hasHit = true;

            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = _damage,
                Position = transform.position,
                IsRanged = true,
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
