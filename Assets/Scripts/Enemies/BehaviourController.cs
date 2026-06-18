using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Drives enemy movement and attack decisions by writing to EnemyData.
    /// Reads neighbour positions from SpatialGrid for separation — no physics queries.
    ///
    /// Design principle: this class never touches the Transform directly.
    /// It writes to EnemyView.Data (a struct), and EnemyView syncs the Transform.
    /// This keeps logic and rendering cleanly separated.
    ///
    /// Attach to: the root of every enemy prefab alongside EnemyView.
    /// </summary>
    [RequireComponent(typeof(EnemyView))]
    public class BehaviourController : MonoBehaviour
    {
        // Injected by EnemyPool after Get() - not serialized
        [System.NonSerialized] public Transform PlayerTransform;
        [System.NonSerialized] public SpatialGrid Grid;
        [System.NonSerialized] public List<EnemyView> ActiveEnemies; // shared ref, do not modify

        // Reused per-frame list — avoids allocation in the hot path
        private readonly List<int> _neighbourIndices = new(16);

        private EnemyView _view;

        void Awake()
        {
            _view = GetComponent<EnemyView>();
        }

        /// <summary>
        /// Called by EnemyPool once per frame for each active enemy.
        /// Keeps Update() off individual enemies — one controlled loop instead of
        /// hundreds of MonoBehaviour Update calls (saves ~1ms on mobile WebGL).
        /// </summary>
        public void Tick(float dt)
        {
            ref var d = ref _view.DataRef;

            //Spawn grace period
            if (d.State == EnemyState.Spawning)
            {
                d.SpawnTimer -= dt;
                if (d.SpawnTimer <= 0f)
                    d.State = EnemyState.Moving;
                return;
            }

            if (!d.IsAlive) return;

            // ── Knockback override ──────────────────────────────────────────
            // While knocked back, AI movement is suspended entirely — the enemy
            // is shoved by the hit, not pathing toward the player. Avoids the
            // two systems fighting over Position in the same frame.
            if (d.IsKnockedBack)
            {
                d.KnockbackTimer -= dt;
                d.Velocity = d.Knockback;
                d.Position += d.Velocity * dt;

                // Decay knockback speed toward zero over its remaining lifetime
                d.Knockback = Vector3.Lerp(d.Knockback, Vector3.zero, dt * 6f);

                if (d.KnockbackTimer <= 0f)
                {
                    d.Knockback = Vector3.zero;
                    d.State = EnemyState.Moving; // resume normal AI next frame
                }
                return;
            }

            if (!PlayerTransform) return;

            Vector3 playerPos = PlayerTransform.position;
            var distToPlayer = Vector3.Distance(d.Position, playerPos);

            //Attack check
            if (distToPlayer <= d.TypeSo.attackRange)
            {
                d.State = EnemyState.Attacking;
                d.AttackTimer -= dt;
                if (d.CanAttack)
                {
                    d.AttackTimer = d.TypeSo.attackCooldown;
                    EventBus.Emit(new EnemyAttackEvent
                    {
                        Damage   = d.Damage,
                        Position = d.Position,
                    });
                }
                d.Velocity = Vector3.zero;
                return;
            }

            //Move toward player
            d.State = EnemyState.Moving;
            var toPlayer = (playerPos - d.Position).normalized;

            // Rotate the seek direction by this enemy's fixed jitter angle (XZ plane only).
            // Small and constant per-enemy, so a pack approaches from a fan of angles
            // instead of every enemy converging on the exact same point — without ever
            // looking like they're failing to chase the player.
            if (Mathf.Abs(d.SeekAngleJitter) > 0.0001f)
            {
                var sin = Mathf.Sin(d.SeekAngleJitter);
                var cos = Mathf.Cos(d.SeekAngleJitter);
                toPlayer = new Vector3(
                    toPlayer.x * cos - toPlayer.z * sin,
                    0f,
                    toPlayer.x * sin + toPlayer.z * cos);
            }

            //Separation from neighbours (flocking)
            // Widened from the old check radius — separation needs to kick in *before*
            // enemies are nearly stacked, or a pack funnels into a single-file line
            // chasing the player instead of fanning out like a swarm.
            const float separationCheckRadius = 2.5f;
            _neighbourIndices.Clear();
            Grid?.GetNeighbourIndices(d.Position, separationCheckRadius, _neighbourIndices);

            var separation = Vector3.zero;
            var weightSum = 0f;
            foreach (var neighbour in from idx in _neighbourIndices
                     where idx >= 0 && idx < ActiveEnemies.Count
                     select ActiveEnemies[idx]
                     into neighbour
                     where neighbour != _view
                     select neighbour)
            {
                var diff = d.Position - neighbour.Data.Position;
                var dist = diff.magnitude;
                if (!(dist > 0.01f) || !(dist < separationCheckRadius)) continue;

                // Inverse-distance weighting: a neighbour right on top of you pushes hard,
                // one near the edge of the check radius barely registers. This is what
                // makes the push feel continuous as enemies approach each other instead
                // of "off" then suddenly "on" at a hard 1.2-unit cutoff.
                var weight = 1f - (dist / separationCheckRadius);
                separation += (diff / dist) * weight;
                weightSum += weight;
            }

            if (weightSum > 0f)
                separation = (separation / weightSum).normalized * d.TypeSo.separationForce;

            // Blend: mostly toward player, repelled from neighbours. Weight bumped up
            // from 0.4 now that separation engages earlier and more gradually — without
            // this the wider radius alone wasn't enough to break up the funnel.
            var desired = (toPlayer + separation * 0.6f).normalized;
            d.Velocity = desired * d.Speed;
            d.Velocity.y = 0f; // lock to XZ plane — no vertical drift from position noise
            d.Position += d.Velocity * dt;
        }

        /// <summary>Called by EnemyView when the enemy takes lethal damage.</summary>
        public void OnDeath()
        {
            ref var d = ref _view.DataRef;
            d.State = EnemyState.Dying;
            // Pool return is handled by EnemyView after death animation completes
        }

        /// <summary>
        /// Applies a knockback impulse, flattened to the XZ plane.
        /// Called by EnemyView.TakeDamage when the hit source carries knockback force.
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force, float duration)
        {
            ref var d = ref _view.DataRef;
            if (!d.IsAlive) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            d.Knockback = direction.normalized * force;
            d.KnockbackTimer = duration;
            d.State = EnemyState.Moving; // exit Attacking state so knockback isn't overridden next frame
        }
    }

    // Placed here since it's only used by BehaviourController
    public struct EnemyAttackEvent
    {
        public float Damage;
        public Vector3 Position;
    }
}