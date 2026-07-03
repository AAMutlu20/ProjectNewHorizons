using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Eye attack: a three-phase laser sequence inspired by Lux R (League of Legends).
    ///
    /// PHASE 1 — WINDUP (windupDuration, default 0.5s):
    ///   Eye locks in place, plays a charge VFX. Direction tracks the player
    ///   live during this phase so the player knows where to expect the beam.
    ///
    /// PHASE 2 — WARNING BEAM (warningDuration, default 0.5s):
    ///   Direction LOCKS to the player's current position at the moment this
    ///   phase starts. A thin orange LineRenderer beam appears — purely visual,
    ///   no hitbox. Player has the full warning duration to step off the line.
    ///
    /// PHASE 3 — FIRE (instant):
    ///   Physics.Raycast along the locked direction on playerLayers.
    ///   If the player is anywhere on the line they take damage or lose a
    ///   shield layer. The beam widens and turns red for flashDuration (0.2s)
    ///   then disappears. Eye returns to cooldown.
    ///
    /// The LineRenderer is driven entirely from this script — no separate VFX
    /// prefab needed. Wire a LineRenderer on this GameObject or a child.
    ///
    /// Attach to: the EyeWinged prefab alongside BehaviourController,
    /// REPLACING EyeRangedAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class EyeLaserAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float windupDuration  = 0.5f;
        [SerializeField] private float warningDuration = 0.5f;
        [SerializeField] private float flashDuration   = 0.2f;

        [Header("Beam visuals")]
        [SerializeField] private LineRenderer beamRenderer;
        [SerializeField] private float warningWidth = 0.08f;
        [SerializeField] private float damageWidth  = 0.35f;
        [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0f, 1f); // orange
        [SerializeField] private Color damageColor  = new Color(1f, 0.1f, 0.1f, 1f);  // red
        [SerializeField] private float beamLength   = 30f;

        [Header("Damage")]
        [SerializeField] private LayerMask playerLayers;

        // Internal state machine
        private enum LaserPhase { Idle, Windup, Warning, Flash }
        private LaserPhase _phase = LaserPhase.Idle;
        private float _phaseTimer;
        private Vector3 _lockedDirection; // set at start of Warning, never changes after

        private BehaviourController _behaviour;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();

            if (!beamRenderer)
                beamRenderer = GetComponentInChildren<LineRenderer>();

            if (beamRenderer)
            {
                beamRenderer.positionCount = 2;
                beamRenderer.useWorldSpace = true;
                beamRenderer.enabled = false;
            }
            else
            {
                Debug.LogWarning("EyeLaserAttackBehaviour: no LineRenderer found. " +
                                 "Add one to this GameObject or a child.", this);
            }
        }

        // Called by BehaviourController every tick while the Eye is within attackRange.
        // We run our own internal phase timer so we don't rely on enemy.AttackTimer
        // for the multi-phase sequence — AttackTimer only gates WHEN the sequence starts.
        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.Velocity = Vector3.zero;

            switch (_phase)
            {
                case LaserPhase.Idle:
                    // Wait for the standard cooldown then begin a new sequence.
                    enemy.AttackTimer -= deltaTime;
                    if (!enemy.CanAttack) return;
                    BeginWindup(ref enemy);
                    break;

                case LaserPhase.Windup:
                    _phaseTimer -= deltaTime;
                    if (_phaseTimer > 0f) return;
                    BeginWarning(enemy.Position);
                    break;

                case LaserPhase.Warning:
                    // Update beam endpoint each tick so it stays the right length
                    // along the locked direction even if this object moves slightly.
                    UpdateBeam(enemy.Position, _lockedDirection, warningWidth, warningColor);
                    _phaseTimer -= deltaTime;
                    if (_phaseTimer > 0f) return;
                    Fire(ref enemy);
                    break;

                case LaserPhase.Flash:
                    UpdateBeam(enemy.Position, _lockedDirection, damageWidth, damageColor);
                    _phaseTimer -= deltaTime;
                    if (_phaseTimer > 0f) return;
                    EndSequence(ref enemy);
                    break;
            }
        }

        private void BeginWindup(ref EnemyData enemy)
        {
            _phase = LaserPhase.Windup;
            _phaseTimer = windupDuration;
            // Reset the cooldown so it won't re-trigger mid-sequence
            enemy.AttackTimer = enemy.TypeSo.attackCooldown;
        }

        private void BeginWarning(Vector3 eyePosition)
        {
            // Lock direction to player's current position — stays fixed for the
            // entire warning and damage phase so the player can learn to dodge.
            if (_behaviour.PlayerTransform)
            {
                var toPlayer = _behaviour.PlayerTransform.position - eyePosition;
                toPlayer.y = 0f; // flatten to XZ — eye fires horizontally
                _lockedDirection = toPlayer.sqrMagnitude > 0.001f
                    ? toPlayer.normalized
                    : transform.forward;
            }
            else
            {
                _lockedDirection = transform.forward;
            }

            _phase = LaserPhase.Warning;
            _phaseTimer = warningDuration;

            if (beamRenderer) beamRenderer.enabled = true;
        }

        private void Fire(ref EnemyData enemy)
        {
            _phase = LaserPhase.Flash;
            _phaseTimer = flashDuration;

            // Raycast along locked direction — damages player if anywhere on the line.
            var origin = enemy.Position + Vector3.up * 0.5f; // slight height so ray isn't flush with floor
            if (Physics.Raycast(origin, _lockedDirection, out _, beamLength, playerLayers))
            {
                EventBus.Emit(new EnemyAttackEvent
                {
                    Damage       = enemy.Damage,
                    Position     = enemy.Position,
                    HitConfirmed = true, // raycast already confirmed player is on the line
                });
            }
        }

        private void EndSequence(ref EnemyData enemy)
        {
            _phase = LaserPhase.Idle;
            if (beamRenderer) beamRenderer.enabled = false;
            // AttackTimer was already reset at BeginWindup — the eye waits a
            // full cooldown before starting the next sequence.
        }

        private void UpdateBeam(Vector3 origin, Vector3 direction, float width, Color color)
        {
            if (!beamRenderer) return;
            var start = origin + Vector3.up * 0.5f;
            var end   = start + direction * beamLength;
            beamRenderer.SetPosition(0, start);
            beamRenderer.SetPosition(1, end);
            beamRenderer.startWidth = width;
            beamRenderer.endWidth   = width;
            beamRenderer.startColor = color;
            beamRenderer.endColor   = color;
        }

        // Reset phase when this enemy is returned to pool and re-used.
        private void OnDisable()
        {
            _phase = LaserPhase.Idle;
            _phaseTimer = 0f;
            if (beamRenderer) beamRenderer.enabled = false;
        }
    }
}
