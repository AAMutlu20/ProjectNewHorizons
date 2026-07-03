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

        [Header("Spawn placement")]
        [Tooltip("Resolves candidate XZ positions into grounded world positions via a ground raycast. " +
                 "Owned by EnemyPool so every spawn path goes through " +
                 "the same resolver automatically.")]
        [SerializeField] private Waves.SpawnPositionResolver spawnPositionResolver;

        [Header("Combat")]
        [Tooltip("Layers treated as solid obstructions for line-of-sight purposes -- an enemy " +
                 "within attackRange but with an obstruction on this layer between it and the " +
                 "player cannot attack. Leave at None/0 to disable obstruction-blocking entirely.")]
        [SerializeField] private LayerMask obstructionLayers;

        [Tooltip("The player's StatSheet -- needed for Bleed/Burn's per-stack crit rolling " +
                 "(each DOT stack rolls crit chance/damage against the player's current stats).")]
        [SerializeField] private Stats.StatSheet playerStatSheet;

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
            Debug.Assert(spawnPositionResolver, "EnemyPool: spawnPositionResolver not assigned");

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
            // Runs on FixedUpdate because ManagedUpdate writes Rigidbody.linearVelocity/
            // calls MoveRotation internally (via EnemyView) — both require physics-tick
            // timing to integrate correctly. Calling them from Update() caused the
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
        /// Retrieve an enemy from the pool and activate it near candidateXz.
        /// The actual spawn Y and final X/Z are resolved by SpawnPositionResolver --
        /// a real downward raycast against world geometry (so raised platforms,
        /// ramps, or floor noise are respected) plus an obstruction check (so the
        /// enemy never spawns inside a wall, a well, or another solid collider).
        /// If the first candidate is obstructed or has no ground beneath it,
        /// retryPositionFunc is called to get a new candidate, up to the
        /// resolver's own retry cap.
        ///
        /// Returns null if the pool for this type is exhausted, the active budget
        /// is full, OR no valid unobstructed grounded position could be found
        /// after all retries.
        ///
        /// isMiniboss is decided by the caller (spawn scheduler / boss-spawn rules) — see
        /// the design doc's miniboss-frequency-after-boss-kills rule.
        /// </summary>
        public EnemyView Get(EnemyType type, Vector3 candidatePos, System.Func<Vector3> retryPositionFunc,
            bool isMiniboss = false)
        {
            if (!_inactive.TryGetValue(type, out var queue) || queue.Count == 0)
            {
                Debug.LogWarning($"EnemyPool: pool exhausted for {type}. Increase poolSize on the SO.");
                return null;
            }

            if (_active.Count >= _currentDiff.Budget)
                return null;

            var so = FindSo(type);
            if (!so) return null;

            // Resolve candidate to a grounded position via downward raycast.
            // No obstruction check — that was blocking all spawns due to
            // unidentified colliders. Ground raycast only.
            if (!spawnPositionResolver.TryResolve(candidatePos, so.groundOffsetY,
                    retryPositionFunc, out var resolvedPosition))
            {
                Debug.LogWarning($"EnemyPool: no ground found for {type} spawn — skipping.");
                return null;
            }

            var view = queue.Dequeue();
            view.gameObject.SetActive(true);
            view.Init(so, _currentDiff, resolvedPosition, playerTransform, spatialGrid, _active,
                isMiniboss, projectilePool, telegraphPool, obstructionLayers, playerStatSheet);
            _active.Add(view);
            return view;
        }

        // Helpers

        /// <summary>Public SO lookup — used by WaveDirector to check canBeMiniboss.</summary>
        public EnemyTypeSo FindSoPublic(EnemyType type) => FindSo(type);

        private EnemyTypeSo FindSo(EnemyType type)
        {
            return enemyTypes.FirstOrDefault(so => so && so.type == type);
        }

        public void Return(EnemyView view)
        {
            var idx = _active.IndexOf(view);
            if (idx < 0) return;

            var last = _active.Count - 1;
            if (idx != last) _active[idx] = _active[last];
            _active.RemoveAt(last);

            view.gameObject.SetActive(false);

            if (_inactive.TryGetValue(view.Type, out var queue))
                queue.Enqueue(view);

            EventBus.Emit(new EnemyReturnedEvent());
        }

        public void ReturnAll()
        {
            var copy = new List<EnemyView>(_active);
            foreach (var v in copy) Return(v);
        }
    }
}
