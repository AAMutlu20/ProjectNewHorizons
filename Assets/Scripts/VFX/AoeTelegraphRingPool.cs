using System.Collections.Generic;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Pre-allocates and recycles AoeTelegraphRing instances, mirroring
    /// EnemyProjectilePool's approach — zero Instantiate/Destroy at runtime
    /// once warmed up.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class AoeTelegraphRingPool : MonoBehaviour
    {
        [SerializeField] private AoeTelegraphRing ringPrefab;
        [SerializeField] private int poolSize = 8;

        private readonly Queue<AoeTelegraphRing> _inactive = new();

        private void Awake()
        {
            Debug.Assert(ringPrefab, "AoeTelegraphRingPool: ringPrefab not assigned.", this);

            for (var i = 0; i < poolSize; i++)
            {
                var ring = Instantiate(ringPrefab, transform);
                ring.Pool = this;
                ring.gameObject.SetActive(false);
                _inactive.Enqueue(ring);
            }
        }

        /// <summary>
        /// Begins a telegraph at worldPosition. Returns false if the pool is
        /// exhausted (rare for boss-frequency attacks — raise poolSize if it fires).
        /// </summary>
        public bool Begin(Vector3 worldPosition, float durationSeconds, float radius, System.Action onComplete)
        {
            if (_inactive.Count == 0)
            {
                Debug.LogWarning("AoeTelegraphRingPool: pool exhausted. Increase poolSize.", this);
                return false;
            }

            var ring = _inactive.Dequeue();
            ring.gameObject.SetActive(true);
            ring.Begin(worldPosition, durationSeconds, radius, onComplete);
            return true;
        }

        /// <summary>Called by AoeTelegraphRing when its countdown finishes.</summary>
        public void Return(AoeTelegraphRing ring)
        {
            ring.gameObject.SetActive(false);
            _inactive.Enqueue(ring);
        }
    }
}
