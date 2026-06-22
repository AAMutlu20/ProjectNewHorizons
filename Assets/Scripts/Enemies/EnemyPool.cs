using System.Collections.Generic;
using System.Linq;
using Core;
using Difficulty;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Pre-allocates all enemy prefabs on Awake. Zero Instantiate or Destroy at runtime.
    /// Also runs the managed update loop for all active enemies — one controlled pass
    /// instead of hundreds of individual MonoBehaviour Update() calls.
    ///
    /// Attach to: EnemyPool.prefab in [Systems].
    /// Wire: enemyTypes array in Inspector, player and grid references via Inspector or GameManager.
    /// </summary>
    public class EnemyPool : MonoBehaviour
    {
        [Header("Enemy types to pre-allocate")]
        [SerializeField] private EnemyTypeSo[] enemyTypes;

        [Header("Scene references")]
        [SerializeField] private Transform  playerTransform;
        [SerializeField] private SpatialGrid spatialGrid;

        [Header("Shared pools (for archetypes that need ranged attacks/telegraphs)")]
        [Tooltip("Wired here, not on individual enemy prefabs, since a prefab asset " +
                 "can't reference a scene object directly. EnemyPool (itself a scene " +
                 "object) holds the reference and forwards it to each spawned enemy.")]
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private VFX.AoeTelegraphRingPool telegraphPool;

        // Pool storage per type
        private Dictionary<EnemyType, Queue<EnemyView>> _inactive;

        // All currently active enemies — SpatialGrid and BehaviourController share this list
        private List<EnemyView> _active;

        // Cached current difficulty — set at wave start
        private DifficultyParams _currentDiff;

        public int ActiveCount => _active.Count;

        /// <summary>
        /// Read-only view of every currently active enemy. Used by player
        /// abilities (Shockwave, Meteor Slam, Poison Aura, etc.) that need to
        /// query "everyone within radius X of the player" — the same data
        /// BehaviourController already gets injected for enemy-side queries
        /// like separation and the buff pulse, exposed here for player-side use.
        /// </summary>
        public IReadOnlyList<EnemyView> ActiveEnemies => _active;

        /// <summary>Read-only access to the spatial grid for radius queries — see ActiveEnemies.</summary>
        public SpatialGrid SpatialGrid => spatialGrid;

        //Lifecycle
        private void Awake()
        {
            Debug.Assert(enemyTypes is { Length: > 0 },
                "EnemyPool: no enemy types assigned");
            Debug.Assert(playerTransform, "EnemyPool: playerTransform not assigned");

            _inactive = new Dictionary<EnemyType, Queue<EnemyView>>();
            _active = new List<EnemyView>(512);

            foreach (var so in enemyTypes)
            {
                if (!so || !so.prefab)
                {
                    Debug.LogWarning("EnemyPool: null entry in enemyTypes — skipping");
                    continue;
                }

                var q = new Queue<EnemyView>(so.poolSize);
                for (var i = 0; i < so.poolSize; i++)
                {
                    var go   = Instantiate(so.prefab, transform);
                    var view = go.GetComponent<EnemyView>();
                    Debug.Assert(view,
                        $"EnemyPool: prefab {so.prefab.name} is missing EnemyView component");

                    view.Pool = this;
                    go.SetActive(false);
                    q.Enqueue(view);
                }
                _inactive[so.type] = q;
                Debug.Log($"EnemyPool: pre-allocated {so.poolSize}× {so.type}");
            }
        }

        private void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;

            // Rebuild spatial grid once before any behaviour reads from it
            spatialGrid?.Rebuild(_active);

            // Single managed update loop — avoids N separate MonoBehaviour Update calls.
            // Runs on FixedUpdate because ManagedUpdate calls Rigidbody.MovePosition/
            // MoveRotation internally (via EnemyView) — those APIs are only meant to be
            // called from the physics tick. Calling them from Update() caused the
            // position/rotation timeline mismatch that produced the stretched capsule look.
            // Iterate backwards so Return() mid-loop (swapped with last) stays safe
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].ManagedUpdate(dt);
            }
        }

        // Public API

        /// <summary>Cache the difficulty params for this wave. Call at wave start.</summary>
        public void SetDifficulty(DifficultyParams diff) => _currentDiff = diff;

        /// <summary>
        /// Retrieve an enemy from the pool and activate it at worldPos.
        /// Returns null if the pool for this type is exhausted (rare — tune poolSize if it fires).
        /// isMiniboss is decided by the caller (spawn scheduler / boss-spawn rules) — see
        /// the design doc's miniboss-frequency-after-boss-kills rule.
        /// </summary>
        public EnemyView Get(EnemyType type, Vector3 worldPos, bool isMiniboss = false)
        {
            if (!_inactive.TryGetValue(type, out var queue) || queue.Count == 0)
            {
                Debug.LogWarning($"EnemyPool: pool exhausted for {type}. " +
                                 $"Increase poolSize on {type} EnemyTypeSO.");
                return null;
            }

            // Respect active budget — don't spawn if we're at the cap
            if (_active.Count >= _currentDiff.Budget)
                return null;

            var view = queue.Dequeue();

            // Find the SO for this type to pass to Init
            var so = FindSo(type);
            if (!so) return null;

            view.gameObject.SetActive(true);
            view.Init(so, _currentDiff, worldPos, playerTransform, spatialGrid, _active, isMiniboss,
                projectilePool, telegraphPool);
            _active.Add(view);
            return view;
        }

        /// <summary>
        /// Return an enemy to the pool. Called by EnemyView after its death animation finishes.
        /// Uses swap-with-last to remove from the middle of _active in O(1).
        /// </summary>
        public void Return(EnemyView view)
        {
            var idx = _active.IndexOf(view);
            if (idx < 0) return; // already returned (safety guard)

            // Swap with last to avoid O(n) shift
            var last = _active.Count - 1;
            if (idx != last)
                _active[idx] = _active[last];
            _active.RemoveAt(last);

            view.gameObject.SetActive(false);

            if (_inactive.TryGetValue(view.Type, out var queue))
                queue.Enqueue(view);

            EventBus.Emit(new EnemyReturnedEvent());
        }

        /// <summary>Return all active enemies to pool immediately. Used on wave skip / debug.</summary>
        public void ReturnAll()
        {
            // Iterate a copy since Return() modifies _active
            var copy = new List<EnemyView>(_active);
            foreach (var v in copy) Return(v);
        }

        // Helpers

        private EnemyTypeSo FindSo(EnemyType type)
        {
            return enemyTypes.FirstOrDefault(so => so && so.type == type);
        }
    }
}
