using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Pre-allocates and recycles DamageOverTimeZone instances, mirroring
    /// EnemyProjectilePool/AoeTelegraphRingPool's approach.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class DamageOverTimeZonePool : MonoBehaviour
    {
        [SerializeField] private DamageOverTimeZone zonePrefab;
        [SerializeField] private int poolSize = 8;

        private readonly Queue<DamageOverTimeZone> _inactive = new();

        private void Awake()
        {
            Debug.Assert(zonePrefab, "DamageOverTimeZonePool: zonePrefab not assigned.", this);

            for (var i = 0; i < poolSize; i++)
            {
                var zone = Instantiate(zonePrefab, transform);
                zone.Pool = this;
                zone.gameObject.SetActive(false);
                _inactive.Enqueue(zone);
            }
        }

        /// <summary>Begins a DOT zone at worldPosition. Returns false if the pool is exhausted.</summary>
        public bool Begin(Vector3 worldPosition, float radius, float tickInterval, float durationSeconds,
            System.Func<EnemyView, float> damagePerSecondFunc, EnemyPool enemyPool)
        {
            if (_inactive.Count == 0)
            {
                Debug.LogWarning("DamageOverTimeZonePool: pool exhausted. Increase poolSize.", this);
                return false;
            }

            var zone = _inactive.Dequeue();
            zone.gameObject.SetActive(true);
            zone.Begin(worldPosition, radius, tickInterval, durationSeconds, damagePerSecondFunc, enemyPool);
            return true;
        }

        /// <summary>Called by DamageOverTimeZone when its duration ends.</summary>
        public void Return(DamageOverTimeZone zone)
        {
            zone.gameObject.SetActive(false);
            _inactive.Enqueue(zone);
        }
    }
}
