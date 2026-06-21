using System.Collections.Generic;
using Player;
using Stats;
using UnityEngine;
using XP;

namespace Enemies
{
    /// <summary>
    /// Pre-allocates and recycles XpOrb instances. Mirrors EnemyProjectilePool's
    /// approach -- zero Instantiate/Destroy at runtime once warmed up.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class XpOrbPool : MonoBehaviour
    {
        [SerializeField] private XpOrb orbPrefab;
        [SerializeField] private int poolSize = 128;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private StatSheet playerStatSheet;
        [SerializeField] private LevelSystem levelSystem;

        private readonly Queue<XpOrb> _inactive = new();

        private void Awake()
        {
            Debug.Assert(orbPrefab, "XpOrbPool: orbPrefab not assigned.", this);
            Debug.Assert(playerTransform, "XpOrbPool: playerTransform not assigned.", this);
            Debug.Assert(playerStatSheet, "XpOrbPool: playerStatSheet not assigned.", this);
            Debug.Assert(levelSystem, "XpOrbPool: levelSystem not assigned.", this);

            for (var i = 0; i < poolSize; i++)
            {
                var orb = Instantiate(orbPrefab, transform);
                orb.Pool = this;
                orb.gameObject.SetActive(false);
                _inactive.Enqueue(orb);
            }
        }

        /// <summary>Spawns an orb at worldPosition carrying xpValue. Silently drops the orb if the pool is exhausted -- see note below.</summary>
        public void Spawn(Vector3 worldPosition, float xpValue)
        {
            if (_inactive.Count == 0)
            {
                // Deliberately a warning, not an error -- at high difficulty with
                // many simultaneous deaths, briefly running out of pooled orbs is
                // a tuning signal (raise poolSize), not a bug. Dropping an orb
                // loses some XP, which is a much better failure mode than a
                // runtime Instantiate spike or an exception.
                Debug.LogWarning("XpOrbPool: pool exhausted, dropping an XP orb. Consider raising poolSize.", this);
                return;
            }

            var orb = _inactive.Dequeue();
            orb.gameObject.SetActive(true);
            orb.Activate(worldPosition, xpValue, playerTransform, playerStatSheet, levelSystem);
        }

        /// <summary>Called by XpOrb when it's collected.</summary>
        public void Return(XpOrb orb)
        {
            orb.gameObject.SetActive(false);
            _inactive.Enqueue(orb);
        }
    }
}
