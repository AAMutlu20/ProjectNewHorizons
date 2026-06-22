using System.Collections.Generic;
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
        private const float SeekJitterEpsilon = 0.0001f;
        private const float KnockbackDecayRate = 6f;
        private const float SeparationCheckRadius = 2.5f;
        private const float SeparationWeight = 0.6f;

        // Injected by EnemyPool after Get() - not serialized
        [System.NonSerialized] public Transform PlayerTransform;
        [System.NonSerialized] public SpatialGrid Grid;
        [System.NonSerialized] public List<EnemyView> ActiveEnemies; // shared ref, do not modify

        // Shared scene-level pools, also injected by EnemyPool. Cannot be
        // serialized directly on the prefab -- a prefab asset can't hold a
        // reference to a scene object (EnemyProjectilePool/AoeTelegraphRingPool
        // live in [Systems] in the scene, not as assets), so archetypes that
        // need them (EyeRangedAttackBehaviour, BossAttackBehaviour) read these
        // instead of having their own [SerializeField] for the pool.
        [System.NonSerialized] public EnemyProjectilePool ProjectilePool;
        [System.NonSerialized] public VFX.AoeTelegraphRingPool TelegraphPool;

        // Reused per-frame list — avoids allocation in the hot path
        private readonly List<int> _neighbourIndices = new(16);

        private EnemyView _view;
        private IEnemyAttackBehaviour _attackBehaviour;
        private IPeriodicAbility _periodicAbility; // optional — not every archetype has one

        private void Awake()
        {
            _view = GetComponent<EnemyView>();
            _attackBehaviour = GetComponent<IEnemyAttackBehaviour>();
            _periodicAbility = GetComponent<IPeriodicAbility>();

            if (_attackBehaviour == null)
                Debug.LogError($"BehaviourController on '{name}' has no IEnemyAttackBehaviour " +
                                "attached — this enemy will move but never attack.", this);
        }

        /// <summary>
        /// Called by EnemyPool once per frame for each active enemy.
        /// Keeps Update() off individual enemies — one controlled loop instead of
        /// hundreds of MonoBehaviour Update calls (saves ~1ms on mobile WebGL).
        /// </summary>
        public void Tick(float deltaTime)
        {
            ref var enemy = ref _view.DataRef;

            if (TickSpawnGracePeriod(ref enemy, deltaTime)) return;
            if (!enemy.IsAlive) return;

            // Stun timer always decays, even while knockback is also active —
            // otherwise a stun applied during a knockback would freeze instead
            // of counting down, since only one "override" state can drive
            // movement per frame but both timers still need to expire.
            TickStunTimer(ref enemy, deltaTime);

            if (TickKnockback(ref enemy, deltaTime)) return;
            if (enemy.IsStunned) { enemy.Velocity = Vector3.zero; return; }

            enemy.TickBuff(deltaTime);
            enemy.TickWeaken(deltaTime);
            enemy.TickSlow(deltaTime);

            // Bleed can kill on its own tick (e.g. a low-HP enemy bleeding out) —
            // re-check IsAlive afterward since TakeDamage may have set State
            // to Dying, in which case the rest of this frame's logic must not run.
            if (enemy.TickBleed(deltaTime, out var bleedDamage))
                _view.TakeDamage(bleedDamage);
            if (!enemy.IsAlive) return;

            // Ambient abilities (summoning, buff pulses, etc.) run regardless of
            // distance to the player — unlike attacks, which are range-gated below.
            _periodicAbility?.TickAbility(ref enemy, deltaTime);

            if (!PlayerTransform) return;

            MoveTowardPlayer(ref enemy, deltaTime);
        }

        /// <summary>Called by EnemyView when the enemy takes lethal damage.</summary>
        public void OnDeath()
        {
            ref var enemy = ref _view.DataRef;
            enemy.State = EnemyState.Dying;
            // Pool return is handled by EnemyView after death animation completes
        }

        /// <summary>
        /// Applies a knockback impulse, flattened to the XZ plane.
        /// Called by EnemyView.TakeDamage when the hit source carries knockback force.
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force, float duration)
        {
            ref var enemy = ref _view.DataRef;
            if (!enemy.IsAlive) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < SeekJitterEpsilon) return;

            enemy.Knockback = direction.normalized * force;
            enemy.KnockbackTimer = duration;
            enemy.State = EnemyState.Moving; // exit Attacking state so knockback isn't overridden next frame
        }

        /// <summary>Returns true if the enemy is still in its spawn grace period (caller should stop ticking).</summary>
        private static bool TickSpawnGracePeriod(ref EnemyData enemy, float deltaTime)
        {
            if (enemy.State != EnemyState.Spawning) return false;

            enemy.SpawnTimer -= deltaTime;
            if (enemy.SpawnTimer <= 0f)
                enemy.State = EnemyState.Moving;

            return true;
        }

        /// <summary>
        /// While knocked back, AI movement is suspended entirely — the enemy
        /// is shoved by the hit, not pathing toward the player. Avoids the
        /// two systems fighting over Position in the same frame.
        /// Returns true if knockback consumed this frame (caller should stop ticking).
        /// </summary>
        private static bool TickKnockback(ref EnemyData enemy, float deltaTime)
        {
            if (!enemy.IsKnockedBack) return false;

            enemy.KnockbackTimer -= deltaTime;
            enemy.Velocity = enemy.Knockback;
            enemy.Position += enemy.Velocity * deltaTime;

            // Decay knockback speed toward zero over its remaining lifetime
            enemy.Knockback = Vector3.Lerp(enemy.Knockback, Vector3.zero, deltaTime * KnockbackDecayRate);

            if (!(enemy.KnockbackTimer <= 0f)) return true;
            enemy.Knockback = Vector3.zero;
            enemy.State = EnemyState.Moving; // resume normal AI next frame

            return true;
        }

        /// <summary>
        /// Decays the stun timer unconditionally — called every tick regardless
        /// of whether knockback is also active, so a stun can't get stuck
        /// frozen behind a knockback that keeps consuming the frame first.
        /// Does not touch Velocity or State; the caller decides what to do
        /// with IsStunned after this runs.
        /// </summary>
        private static void TickStunTimer(ref EnemyData enemy, float deltaTime)
        {
            if (!enemy.IsStunned) return;

            enemy.StunTimer -= deltaTime;
            if (enemy.StunTimer <= 0f)
                enemy.State = EnemyState.Moving; // resume normal AI next frame
        }

        private void MoveTowardPlayer(ref EnemyData enemy, float deltaTime)
        {
            var playerPosition = PlayerTransform.position;
            var distanceToPlayer = Vector3.Distance(enemy.Position, playerPosition);

            if (distanceToPlayer <= enemy.TypeSo.attackRange)
            {
                _attackBehaviour?.TickAttack(ref enemy, deltaTime);
                return;
            }

            enemy.State = EnemyState.Moving;

            var seekDirection = GetJitteredSeekDirection(enemy, playerPosition);
            var separation = GetSeparationFromNeighbours(enemy);

            var desiredDirection = (seekDirection + separation * SeparationWeight).normalized;
            enemy.Velocity = desiredDirection * enemy.Speed;
            enemy.Velocity.y = 0f; // lock to XZ plane — no vertical drift from position noise
            enemy.Position += enemy.Velocity * deltaTime;
        }

        /// <summary>
        /// Rotates the seek-toward-player direction by this enemy's fixed jitter
        /// angle (XZ plane only). Small and constant per-enemy, so a pack
        /// approaches from a fan of angles instead of every enemy converging on
        /// the exact same point — without ever looking like they're failing to
        /// chase the player.
        /// </summary>
        private static Vector3 GetJitteredSeekDirection(EnemyData enemy, Vector3 playerPosition)
        {
            var towardPlayer = (playerPosition - enemy.Position).normalized;

            if (Mathf.Abs(enemy.SeekAngleJitter) <= SeekJitterEpsilon)
                return towardPlayer;

            var sin = Mathf.Sin(enemy.SeekAngleJitter);
            var cos = Mathf.Cos(enemy.SeekAngleJitter);
            return new Vector3(
                towardPlayer.x * cos - towardPlayer.z * sin,
                0f,
                towardPlayer.x * sin + towardPlayer.z * cos);
        }

        /// <summary>
        /// Computes a separation force away from nearby enemies (flocking).
        /// Uses inverse-distance weighting: a neighbour right on top of you
        /// pushes hard, one near the edge of the check radius barely registers —
        /// this keeps the push feeling continuous as enemies approach each
        /// other instead of "off" then suddenly "on" at a hard cutoff.
        /// </summary>
        private Vector3 GetSeparationFromNeighbours(EnemyData enemy)
        {
            _neighbourIndices.Clear();
            Grid?.GetNeighbourIndices(enemy.Position, SeparationCheckRadius, _neighbourIndices);

            var separation = Vector3.zero;
            var weightSum = 0f;

            foreach (var neighbourIndex in _neighbourIndices)
            {
                if (neighbourIndex < 0 || neighbourIndex >= ActiveEnemies.Count) continue;

                var neighbour = ActiveEnemies[neighbourIndex];
                if (neighbour == _view) continue;

                var offsetFromNeighbour = enemy.Position - neighbour.Data.Position;
                var distance = offsetFromNeighbour.magnitude;
                if (distance is <= 0.01f or >= SeparationCheckRadius) continue;

                var weight = 1f - (distance / SeparationCheckRadius);
                separation += (offsetFromNeighbour / distance) * weight;
                weightSum += weight;
            }

            if (weightSum <= 0f) return Vector3.zero;

            return (separation / weightSum).normalized * enemy.TypeSo.separationForce;
        }
    }
}
