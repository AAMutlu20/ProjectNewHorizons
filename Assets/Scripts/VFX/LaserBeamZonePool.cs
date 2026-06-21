using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Pre-allocates and recycles LaserBeamZone instances. Up to 4 beams can
    /// be active simultaneously at Legendary, so poolSize should be at least
    /// that plus headroom for cooldown overlap edge cases.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class LaserBeamZonePool : MonoBehaviour
    {
        [SerializeField] private LaserBeamZone beamPrefab;
        [SerializeField] private int poolSize = 6;

        private readonly Queue<LaserBeamZone> _inactive = new();

        private void Awake()
        {
            Debug.Assert(beamPrefab, "LaserBeamZonePool: beamPrefab not assigned.", this);

            for (var i = 0; i < poolSize; i++)
            {
                var beam = Instantiate(beamPrefab, transform);
                beam.Pool = this;
                beam.gameObject.SetActive(false);
                _inactive.Enqueue(beam);
            }
        }

        /// <summary>Begins a beam. Returns false if the pool is exhausted.</summary>
        public bool Begin(Transform followTarget, Vector3 direction, float beamWidth, float damagePercentMaxHpPerSecond,
            float weakenMultiplier, float durationSeconds, float rotationDegreesPerSecond, EnemyPool enemyPool)
        {
            if (_inactive.Count == 0)
            {
                Debug.LogWarning("LaserBeamZonePool: pool exhausted. Increase poolSize.", this);
                return false;
            }

            var beam = _inactive.Dequeue();
            beam.gameObject.SetActive(true);
            beam.Begin(followTarget, direction, beamWidth, damagePercentMaxHpPerSecond,
                weakenMultiplier, durationSeconds, rotationDegreesPerSecond, enemyPool);
            return true;
        }

        /// <summary>Called by LaserBeamZone when its duration ends.</summary>
        public void Return(LaserBeamZone beam)
        {
            beam.gameObject.SetActive(false);
            _inactive.Enqueue(beam);
        }
    }
}
