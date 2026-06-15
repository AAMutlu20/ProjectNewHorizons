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
            if (!PlayerTransform) return;

            Vector2 playerPos = PlayerTransform.position;
            var distToPlayer = Vector2.Distance(d.Position, playerPos);

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
                d.Velocity = Vector2.zero;
                return;
            }

            //Move toward player
            d.State = EnemyState.Moving;
            var toPlayer = (playerPos - d.Position).normalized;

            //Separation from neighbours (flocking)
            _neighbourIndices.Clear();
            Grid?.GetNeighbourIndices(d.Position, d.TypeSo.separationForce * 1.5f, _neighbourIndices);

            var separation = Vector2.zero;
            var count = 0;
            foreach (var neighbour in from idx in _neighbourIndices
                     where idx >= 0 && idx < ActiveEnemies.Count
                     select ActiveEnemies[idx]
                     into neighbour
                     where neighbour != _view
                     select neighbour)
            {
                var diff = d.Position - neighbour.Data.Position;
                var dist = diff.magnitude;
                if (!(dist > 0.01f) || !(dist < 1.2f)) continue; // only push if very close
                separation += diff / dist;// weighted by inverse distance
                count++;
            }

            if (count > 0)
                separation = (separation / count).normalized * d.TypeSo.separationForce;

            // Blend: mostly toward player, slightly repelled from neighbours
            var desired = (toPlayer + separation * 0.4f).normalized;
            d.Velocity = desired * d.Speed;
            d.Position += d.Velocity * dt;
        }

        /// <summary>Called by EnemyView when the enemy takes lethal damage.</summary>
        public void OnDeath()
        {
            ref var d = ref _view.DataRef;
            d.State = EnemyState.Dying;
            // Pool return is handled by EnemyView after death animation completes
        }
    }

    // Placed here since it's only used by BehaviourController
    public struct EnemyAttackEvent
    {
        public float Damage;
        public Vector2 Position;
    }
}