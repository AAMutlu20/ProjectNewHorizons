using Core;
using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// One Laser Beam instance: an infinite-range line from origin in
    /// direction, damaging and weakening every alive enemy within beamWidth
    /// of that line. If rotationDegreesPerSecond is non-zero (Legendary),
    /// the direction slowly rotates around the Y axis for the beam's duration.
    ///
    /// Weaken is reapplied every damage tick with a duration slightly longer
    /// than the tick interval — so it stays active continuously while an
    /// enemy remains in the beam, but expires promptly (within ~1 tick) once
    /// they leave, rather than lasting the beam's entire remaining lifetime.
    ///
    /// Pooled by LaserBeamZonePool — never Instantiate/Destroy this directly.
    /// </summary>
    public class LaserBeamZone : MonoBehaviour
    {
        private const float TickInterval = 1f; // doc's "X%maxhp/s" implies a per-second tick, matching DamageOverTimeZone's convention
        private const float WeakenDurationPerTick = TickInterval * 1.5f; // outlasts one tick so it doesn't flicker off between ticks

        [System.NonSerialized] public LaserBeamZonePool Pool;

        private Vector3 _direction;
        private float _beamWidth;
        private float _damagePercentMaxHpPerSecond;
        private float _weakenMultiplier;
        private float _rotationDegreesPerSecond;

        private float _remainingDuration;
        private float _tickTimer;
        private bool _isActive;
        private EnemyPool _enemyPool;
        private Transform _followTarget;

        /// <summary>
        /// Starts the beam, following followTarget's position every frame
        /// (the doc doesn't specify whether the beam tracks the player as
        /// they move, but a beam fixed at cast-time position would become
        /// irrelevant almost immediately in a game where the player is
        /// always moving — tracking is the sensible read).
        /// </summary>
        public void Begin(Transform followTarget, Vector3 direction, float beamWidth, float damagePercentMaxHpPerSecond,
            float weakenMultiplier, float durationSeconds, float rotationDegreesPerSecond, EnemyPool enemyPool)
        {
            _followTarget = followTarget;
            _direction = direction.normalized;
            _beamWidth = beamWidth;
            _damagePercentMaxHpPerSecond = damagePercentMaxHpPerSecond;
            _weakenMultiplier = weakenMultiplier;
            _rotationDegreesPerSecond = rotationDegreesPerSecond;

            _remainingDuration = durationSeconds;
            _tickTimer = 0f; // tick immediately, then on interval — matches DamageOverTimeZone
            _isActive = true;
            _enemyPool = enemyPool;

            UpdateTransform();
        }

        private Vector3 Origin => _followTarget ? _followTarget.position : transform.position;

        private void Update()
        {
            if (!_isActive) return;

            if (_rotationDegreesPerSecond != 0f)
                _direction = Quaternion.Euler(0f, _rotationDegreesPerSecond * Time.deltaTime, 0f) * _direction;

            UpdateTransform(); // origin tracks the player every frame regardless of rotation

            _remainingDuration -= Time.deltaTime;
            if (_remainingDuration <= 0f)
            {
                Deactivate();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;

            _tickTimer = TickInterval;
            DamageAndWeakenEnemiesInBeam();
        }

        /// <summary>Keeps the transform aligned with the beam's current origin/direction, for VFX (a renderer child) to follow.</summary>
        private void UpdateTransform()
        {
            transform.position = Origin;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void DamageAndWeakenEnemiesInBeam()
        {
            foreach (var enemyView in _enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;
                if (!IsInsideBeam(enemyView.Data.Position)) continue;

                var damagePerTick = enemyView.Data.MaxHp * _damagePercentMaxHpPerSecond * TickInterval;
                enemyView.TakeDamage(damagePerTick);
                enemyView.DataRef.ApplyWeaken(_weakenMultiplier, WeakenDurationPerTick);
            }
        }

        /// <summary>
        /// True if position is within beamWidth of the infinite ray from
        /// Origin in _direction, and on the forward side of the origin
        /// (the beam doesn't extend backward behind the player).
        /// </summary>
        private bool IsInsideBeam(Vector3 position)
        {
            var origin = Origin;
            var toPosition = position - origin;
            var forwardDistance = Vector3.Dot(toPosition, _direction);
            if (forwardDistance < 0f) return false;

            var closestPointOnRay = origin + _direction * forwardDistance;
            var perpendicularDistance = Vector3.Distance(position, closestPointOnRay);
            return perpendicularDistance <= _beamWidth;
        }

        private void Deactivate()
        {
            _isActive = false;
            Pool.Return(this);
        }
    }
}
