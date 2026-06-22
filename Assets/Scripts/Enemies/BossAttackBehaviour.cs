using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Boss attack: alternates between two telegraphed AOE patterns per the
    /// design doc -- a line of projectiles toward the player, and a ground
    /// slam centred on the boss. Both show a telegraph ring before landing.
    ///
    /// Reads both pools from BehaviourController.ProjectilePool/TelegraphPool
    /// (injected by EnemyPool at spawn time) rather than its own
    /// [SerializeField]s -- a prefab asset can't reference EnemyProjectilePool
    /// or AoeTelegraphRingPool directly since both live in the scene, not as
    /// assets. Wire both pools on EnemyPool itself instead.
    ///
    /// Unlike other archetypes, this does NOT use enemy.AttackTimer/CanAttack --
    /// the boss's pace is driven by its own attackCycleInterval, since it has
    /// two distinct attacks to alternate between rather than one repeating hit.
    ///
    /// Each attack sets enemy.AttackId (GroundSlamAttackId / LineAttackId) so
    /// the Animator Controller can branch to the matching clip instead of
    /// both attacks sharing one generic "Attacking" animation.
    ///
    /// By default the telegraph ring's own duration decides when the attack
    /// actually lands. If useAnimationEventForRelease is enabled (once a real
    /// rig exists), the telegraph still plays for its full duration as visual
    /// warning, but the damage/projectile effect waits for OnAttackAnimationEvent
    /// instead -- so it lands exactly on the swing/impact frame of the clip
    /// rather than on a flat timer.
    ///
    /// Attach to: the Boss prefab, alongside BehaviourController, instead of
    /// MeleeAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class BossAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        public const int GroundSlamAttackId = 1;
        public const int LineAttackId = 2;

        // Must match the string set on the Animation Event in the clip editor.
        private const string ReleaseAnimationEventName = "Release";

        [Header("Pacing")]
        [SerializeField] private float attackCycleInterval = 6f;
        [SerializeField] private float telegraphDuration = 1.5f;

        [Header("Animation sync")]
        [Tooltip("Once a real rig exists: enable to have the attack land on an Animation Event " +
                 "instead of the telegraph ring's own timer. Leave off for graybox testing.")]
        [SerializeField] private bool useAnimationEventForRelease;

        [Tooltip("Safety fallback: if the Animation Event never fires (missing/misnamed event " +
                 "in the clip), the pending effect fires anyway after this many seconds so the " +
                 "boss can't get stuck permanently mid-attack.")]
        [SerializeField] private float animationEventTimeoutSeconds = 3f;

        [Header("Line attack")]
        [SerializeField] private int lineProjectileCount = 5;
        [SerializeField] private float lineProjectileSpacing = 1.5f;
        [SerializeField] private float lineProjectileSpeed = 14f;
        [SerializeField] private float lineTelegraphRadius = 1f;

        [Header("Ground slam")]
        [SerializeField] private float slamRadius = 8f;

        private BehaviourController _behaviour;
        private float _cycleTimer;
        private bool _isAttackInProgress;
        private bool _nextAttackIsSlam;

        // Holds the pending effect while waiting for an Animation Event, when
        // useAnimationEventForRelease is enabled. Null whenever nothing's pending.
        private System.Action _pendingReleaseEffect;
        private float _pendingReleaseTimeoutTimer;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
            _cycleTimer = attackCycleInterval;
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.Velocity = Vector3.zero;

            TickPendingReleaseTimeout(deltaTime);

            if (_isAttackInProgress) return;

            _cycleTimer -= deltaTime;
            if (_cycleTimer > 0f) return;

            _cycleTimer = attackCycleInterval;
            BeginNextAttack(ref enemy);
        }

        private void TickPendingReleaseTimeout(float deltaTime)
        {
            if (_pendingReleaseEffect == null) return;

            _pendingReleaseTimeoutTimer -= deltaTime;
            if (_pendingReleaseTimeoutTimer > 0f) return;

            Debug.LogWarning($"BossAttackBehaviour on '{name}': Animation Event '{ReleaseAnimationEventName}' " +
                              "never fired before timeout -- firing the attack effect anyway so the boss " +
                              "doesn't get stuck. Check the attack clip has the event authored correctly.", this);

            var effect = _pendingReleaseEffect;
            _pendingReleaseEffect = null;
            effect();
        }

        /// <summary>
        /// Called by EnemyAnimationEventReceiver when the attack clip reaches
        /// its release frame. Only does anything if useAnimationEventForRelease
        /// is enabled and an effect is actually waiting on it.
        /// </summary>
        public void OnAttackAnimationEvent(string eventName)
        {
            if (!useAnimationEventForRelease) return;
            if (eventName != ReleaseAnimationEventName) return;
            if (_pendingReleaseEffect == null) return;

            var effect = _pendingReleaseEffect;
            _pendingReleaseEffect = null;
            effect();
        }

        private void BeginNextAttack(ref EnemyData enemy)
        {
            _isAttackInProgress = true;
            _nextAttackIsSlam = !_nextAttackIsSlam; // alternate every cycle

            enemy.AttackId = _nextAttackIsSlam ? GroundSlamAttackId : LineAttackId;

            if (_nextAttackIsSlam)
                BeginGroundSlam(enemy);
            else
                BeginLineAttack(enemy);
        }

        private void BeginGroundSlam(EnemyData enemy)
        {
            if (!_behaviour.TelegraphPool)
            {
                Debug.LogError("BossAttackBehaviour: BehaviourController.TelegraphPool is not set -- " +
                                "wire telegraphPool on EnemyPool in the scene.", this);
                _isAttackInProgress = false;
                return;
            }

            _behaviour.TelegraphPool.Begin(enemy.Position, telegraphDuration, slamRadius,
                () => ResolveReleaseEffect(() => LandGroundSlam(enemy)));
        }

        private void BeginLineAttack(EnemyData enemy)
        {
            if (!_behaviour.TelegraphPool || !_behaviour.ProjectilePool)
            {
                Debug.LogError("BossAttackBehaviour: BehaviourController.TelegraphPool or ProjectilePool " +
                                "is not set -- wire both on EnemyPool in the scene.", this);
                _isAttackInProgress = false;
                return;
            }

            if (!_behaviour.PlayerTransform)
            {
                _isAttackInProgress = false;
                return;
            }

            var directionToPlayer = (_behaviour.PlayerTransform.position - enemy.Position).normalized;
            _behaviour.TelegraphPool.Begin(enemy.Position, telegraphDuration, lineTelegraphRadius,
                () => ResolveReleaseEffect(() => FireProjectileLine(enemy, directionToPlayer)));
        }

        /// <summary>
        /// Either fires the effect immediately (telegraph-timer mode) or holds
        /// it until OnAttackAnimationEvent calls it back (animation-event mode).
        /// </summary>
        private void ResolveReleaseEffect(System.Action effect)
        {
            if (!useAnimationEventForRelease)
            {
                effect();
                return;
            }

            _pendingReleaseEffect = effect;
            _pendingReleaseTimeoutTimer = animationEventTimeoutSeconds;
        }

        private void LandGroundSlam(EnemyData enemy)
        {
            if (!_behaviour.PlayerTransform) { _isAttackInProgress = false; return; }

            var distanceToPlayer = Vector3.Distance(enemy.Position, _behaviour.PlayerTransform.position);
            if (distanceToPlayer <= slamRadius)
            {
                EventBus.Emit(new EnemyAttackEvent
                {
                    Damage = enemy.Damage,
                    Position = _behaviour.PlayerTransform.position,
                });
            }

            _isAttackInProgress = false;
        }

        private void FireProjectileLine(EnemyData enemy, Vector3 direction)
        {
            for (var i = 0; i < lineProjectileCount; i++)
            {
                var spawnOffset = direction * (lineProjectileSpacing * i);
                _behaviour.ProjectilePool.Fire(enemy.Position + spawnOffset, direction, lineProjectileSpeed, enemy.Damage, 5f);
            }

            _isAttackInProgress = false;
        }
    }
}
