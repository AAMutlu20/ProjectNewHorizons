using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Pre-allocates and recycles enemy projectiles, mirroring EnemyPool's
    /// zero-Instantiate-at-runtime approach. Any ranged attack behaviour
    /// (Eye, Boss) calls Fire() instead of Instantiate-ing its own bullets.
    ///
    /// Attach to: a GameObject in [Systems], alongside EnemyPool.
    /// Wire: projectilePrefab must have an EnemyProjectile component.
    /// </summary>
    public class EnemyProjectilePool : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private int poolSize = 64;

        private readonly Queue<EnemyProjectile> _inactive = new();

        private void Awake()
        {
            Debug.Assert(projectilePrefab, "EnemyProjectilePool: projectilePrefab not assigned.", this);

            for (var i = 0; i < poolSize; i++)
            {
                var projectile = Instantiate(projectilePrefab, transform);
                projectile.Pool = this;
                projectile.gameObject.SetActive(false);
                _inactive.Enqueue(projectile);
            }
        }

        /// <summary>Fires a projectile from origin toward direction. Returns false if the pool is exhausted.</summary>
        public bool Fire(Vector3 origin, Vector3 direction, float speed, float damage, float lifetimeSeconds)
        {
            if (_inactive.Count == 0)
            {
                Debug.LogWarning("EnemyProjectilePool: pool exhausted. Increase poolSize.", this);
                return false;
            }

            var projectile = _inactive.Dequeue();
            projectile.gameObject.SetActive(true);
            projectile.Launch(origin, direction, speed, damage, lifetimeSeconds);
            return true;
        }

        /// <summary>Called by EnemyProjectile when it hits something or expires.</summary>
        public void Return(EnemyProjectile projectile)
        {
            projectile.gameObject.SetActive(false);
            _inactive.Enqueue(projectile);
        }
    }
}
