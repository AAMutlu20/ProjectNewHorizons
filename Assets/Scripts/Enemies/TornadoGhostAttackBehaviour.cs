using Core;
using Player;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Tornado Ghost attack: a telegraphed pull-and-damage sequence, NOT a
    /// continuous every-tick drag. Per the design doc (Section 7.4/7.5):
    ///
    ///   WINDUP  → a filling telegraph ring shows for windupDuration seconds
    ///   EXECUTE → pull fires once atomically: forced displacement toward the
    ///             ghost + a single damage hit resolved at pull's end position
    ///   RECOVER → brief post-attack window before the next cycle
    ///
    /// The old implementation called PullPlayerTowardSelf() every tick while
    /// in attack range, producing a continuous drag that had no telegraph,
    /// no player read, and no recovery window. The new version uses the
    /// existing AoeTelegraphRing (the filling-ring VFX already in use for
    /// the Boss's AOE attacks) to show the pull's windup, then fires once.
    ///
    /// Attach to: the TornadoGhost prefab alongside BehaviourController.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class TornadoGhostAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        [Header("Pull")]
        [SerializeField] private float pullStrength = 8f;

        [Header("Timing")]
        [SerializeField] private float windupDuration = 1.2f;  // telegraph ring fill duration — long enough to react
        [SerializeField] private float recoverDuration = 0.8f; // post-attack recovery before next cycle

        [Header("Telegraph")]
        [SerializeField] private float telegraphRadius = 1.5f; // visual ring size around ghost

        private enum PullState { Idle, Windup, Recover }

        private BehaviourController _behaviour;
        private PlayerController _cachedPlayerController;
        private PullState _pullState = PullState.Idle;
        private float _stateTimer;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.Velocity = Vector3.zero;

            switch (_pullState)
            {
                case PullState.Idle:
                    BeginWindup(enemy);
                    break;

                case PullState.Windup:
                    _stateTimer -= deltaTime;
                    if (_stateTimer <= 0f)
                        ExecutePull(ref enemy);
                    break;

                case PullState.Recover:
                    _stateTimer -= deltaTime;
                    if (_stateTimer <= 0f)
                        _pullState = PullState.Idle;
                    break;
            }
        }

        private void BeginWindup(EnemyData enemy)
        {
            _pullState = PullState.Windup;
            _stateTimer = windupDuration;

            // Show a telegraph ring on the ghost itself (not the player's position)
            // so the player can read "this enemy is about to pull me."
            // Uses the cool-tint (control/pull) convention from the design doc —
            // but we reuse the same AoeTelegraphRingPool already wired to the
            // BehaviourController. The ring's onComplete callback is intentionally
            // left null here: ExecutePull is driven by the timer, not the ring,
            // so the two systems can't desync even if the ring fires slightly late.
            _behaviour.TelegraphPool?.Begin(enemy.Position, windupDuration, telegraphRadius, onComplete: null);
        }

        private void ExecutePull(ref EnemyData enemy)
        {
            _pullState = PullState.Recover;
            _stateTimer = recoverDuration;

            if (!_behaviour.PlayerTransform) return;

            var player = GetPlayerController();
            if (player == null) return;

            // Forced displacement: push player toward ghost along the XZ plane.
            // Uses PlayerController.ApplyExternalPull rather than Rigidbody.AddForce —
            // per the design doc Section 7.4, forced displacement must be a dedicated
            // override that takes priority over input, not a physics push that can be
            // absorbed by other colliders mid-pull.
            var toGhost = enemy.Position - _behaviour.PlayerTransform.position;
            toGhost.y = 0f;
            if (toGhost.sqrMagnitude < 0.0001f) return;

            player.ApplyExternalPull(toGhost.normalized * pullStrength);

            // Single damage hit at the moment the pull fires.
            // Damage is resolved here (not at arrival) — close enough given
            // pullDuration is short and the event bus handles the rest.
            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = enemy.Damage,
                Position = enemy.Position,
            });
        }

        private PlayerController GetPlayerController()
        {
            if (!_cachedPlayerController && _behaviour.PlayerTransform)
                _cachedPlayerController = _behaviour.PlayerTransform.GetComponent<PlayerController>();
            return _cachedPlayerController;
        }
    }
}
