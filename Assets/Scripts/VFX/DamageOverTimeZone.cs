using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// A persistent area that damages every alive enemy inside it on a tick
    /// interval, for a fixed duration, then deactivates. Shared infrastructure
    /// for any ability with a damage-over-time zone — Meteor Slam's lava
    /// pool, Poison Aura, Cone of Fire's residual burn.
    ///
    /// damagePerSecondFunc can either return a flat amount or be computed
    /// per-enemy (e.g. percent of that enemy's own max HP) — it receives the
    /// target EnemyView so callers can read its MaxHp themselves rather than
    /// this class needing to know about percent-based formulas.
    ///
    /// Pooled by DamageOverTimeZonePool — never Instantiate/Destroy this directly.
    /// </summary>
    public class DamageOverTimeZone : MonoBehaviour
    {
        [System.NonSerialized] public DamageOverTimeZonePool Pool;

        private float _radius;
        private float _tickInterval;
        private float _remainingDuration;
        private float _tickTimer;
        private bool _isActive;
        private EnemyPool _enemyPool;
        private System.Func<EnemyView, float> _damagePerSecondFunc;

        /// <summary>
        /// Starts the zone at worldPosition. damagePerSecondFunc is called
        /// once per affected enemy per tick to compute that tick's damage —
        /// e.g. (enemy) => enemy.Data.MaxHp * 0.10f for a percent-maxHP burn.
        /// </summary>
        public void Begin(Vector3 worldPosition, float radius, float tickInterval, float durationSeconds,
            System.Func<EnemyView, float> damagePerSecondFunc, EnemyPool enemyPool)
        {
            transform.position = worldPosition;

            _radius = radius;
            _tickInterval = tickInterval;
            _remainingDuration = durationSeconds;
            _tickTimer = 0f; // tick immediately on the first frame, then on interval
            _isActive = true;
            _damagePerSecondFunc = damagePerSecondFunc;
            _enemyPool = enemyPool;
        }

        private void Update()
        {
            if (!_isActive) return;

            _remainingDuration -= Time.deltaTime;
            if (_remainingDuration <= 0f)
            {
                Deactivate();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;

            _tickTimer = _tickInterval;
            DamageEnemiesInRadius();
        }

        private void DamageEnemiesInRadius()
        {
            foreach (var enemyView in _enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(transform.position, enemyView.Data.Position);
                if (distance > _radius) continue;

                var damagePerSecond = _damagePerSecondFunc(enemyView);
                enemyView.TakeDamage(damagePerSecond * _tickInterval);
            }
        }

        private void Deactivate()
        {
            _isActive = false;
            Pool.Return(this);
        }
    }
}
