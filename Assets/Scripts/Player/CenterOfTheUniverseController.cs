using Core;
using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Center of the Universe: once granted, a black hole follows above the
    /// player permanently, growing in radius and execute threshold with every
    /// enemy kill anywhere on the map (not just its own executes). Instantly
    /// kills any enemy inside its radius whose HP is below the current
    /// execute threshold.
    ///
    /// Like Dark Shield/Poison Aura, this is a persistent state, not a
    /// cast-on-cooldown ability — granted once via GrantBlackHole and never
    /// recast. Only Epic/Legendary exist; whatever grants this must never
    /// call GrantBlackHole for Common/Rare.
    ///
    /// Attach to: a SEPARATE GameObject (e.g. "BlackHoleVisual"), NOT
    /// PlayerRoot itself — this script moves its own transform to hover
    /// above playerTransform every frame, which would conflict with the
    /// player's own transform if they were the same object.
    /// </summary>
    public class CenterOfTheUniverseController : MonoBehaviour
    {
        private const float ExecuteCheckInterval = 0.25f; // checking every frame is wasteful for a slow-growing effect

        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private Transform playerTransform;
        [Tooltip("Height above the player's feet (ground level) where the black hole sits. " +
                 "0 = ground level. Keep small so it doesn't block the top-down camera view. " +
                 "playerFeetOffset accounts for the capsule centre being above the floor.")]
        [SerializeField] private float hoverHeight = 0f;

        [Tooltip("How far below playerTransform.position the player's feet actually are. " +
                 "Equal to half the capsule height. Default 1 for a scale-1 capsule. " +
                 "If your player is scaled 2x this should be 2.")]
        [SerializeField] private float playerFeetOffset = 1f;
        [Tooltip("The black hole visual prefab (mesh, plane, sphere — any GameObject). " +
                 "Instantiated once on grant and scaled to match CurrentRadius every frame.")]
        [SerializeField] private UnityEngine.GameObject blackHolePrefab;
        [Tooltip("The prefab's visual radius at localScale = 1.")]
        [SerializeField] private float visualBaseRadius = 1f;

        private float _baseSize;
        private float _sizeScalingPerEnemy;
        private float _executeScalingPerEnemy;
        private float _executeThresholdPercent;

        private int _totalKillsObserved;
        private float _checkTimer;
        private bool _isGranted;
        private UnityEngine.GameObject _visualInstance;

        public bool IsGranted => _isGranted;
        public float CurrentRadius => _baseSize + _sizeScalingPerEnemy * _totalKillsObserved;
        public float CurrentExecuteThresholdPercent => _executeThresholdPercent + _executeScalingPerEnemy * _totalKillsObserved;

        private void Awake()
        {
            Debug.Assert(enemyPool, "CenterOfTheUniverseController: enemyPool not assigned.", this);
            Debug.Assert(playerTransform, "CenterOfTheUniverseController: playerTransform not assigned.", this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnAnyEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnAnyEnemyDied);
        }

        private void Update()
        {
            if (!_isGranted) return;

            // Position at player feet (playerTransform.position - playerFeetOffset) + hoverHeight.
            // playerTransform.position is the Rigidbody centre (capsule midpoint), not the floor.
            // Subtracting playerFeetOffset brings us to ground level first.
            var feetPosition = playerTransform.position - Vector3.up * playerFeetOffset;
            transform.position = feetPosition + Vector3.up * hoverHeight;

            // Scale the visual to match the current gameplay radius every frame.
            // visualBaseRadius is the prefab's authored radius at scale 1 — dividing
            // CurrentRadius by it gives the correct uniform scale factor.
            if (_visualInstance)
            {
                var scale = visualBaseRadius > 0f ? CurrentRadius / visualBaseRadius : CurrentRadius;
                _visualInstance.transform.localScale = UnityEngine.Vector3.one * scale;
            }

            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = ExecuteCheckInterval;
            ExecuteEnemiesBelowThreshold();
        }

        /// <summary>Grants the black hole at the given tier's base stats. Call only for Epic/Legendary.</summary>
        public void GrantBlackHole(Abilities.CenterOfTheUniverseStats stats)
        {
            _baseSize = stats.BaseSize;
            _sizeScalingPerEnemy = stats.SizeScalingPerEnemy;
            _executeScalingPerEnemy = stats.ExecuteScalingPerEnemy;
            _executeThresholdPercent = stats.ExecuteThresholdPercent;

            if (blackHolePrefab && _visualInstance == null)
            {
                _visualInstance = UnityEngine.Object.Instantiate(blackHolePrefab, transform);
                _visualInstance.transform.localPosition = UnityEngine.Vector3.zero;
            }
            _isGranted = true;
        }

        // Every kill anywhere on the map feeds growth, regardless of cause —
        // melee, other abilities, even this ability's own executes route
        // through the same EnemyDiedEvent, so no special-casing is needed
        // to avoid double-counting.
        private void OnAnyEnemyDied(EnemyDiedEvent enemyDied)
        {
            if (!_isGranted) return;
            _totalKillsObserved++;
        }

        private void ExecuteEnemiesBelowThreshold()
        {
            var radius = CurrentRadius;
            var thresholdFraction = CurrentExecuteThresholdPercent / 100f;
            var blackHolePosition = transform.position;

            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(blackHolePosition, enemyView.Data.Position);
                if (distance > radius) continue;

                var hpFraction = enemyView.Data.MaxHp > 0f ? enemyView.Data.Hp / enemyView.Data.MaxHp : 0f;
                if (hpFraction > thresholdFraction) continue;

                // Execute: deal exactly the enemy's current HP as damage, guaranteeing
                // the kill regardless of any damage-reduction-style effects, since this
                // is meant to be an unconditional instant kill, not a damage instance.
                enemyView.TakeDamage(enemyView.Data.Hp);
            }
        }
    }
}
