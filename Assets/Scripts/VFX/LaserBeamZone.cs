using Core;
using Enemies;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// One Laser Beam instance: an infinite-range line from origin in
    /// direction, damaging and weakening every alive enemy within beamWidth
    /// of that line. At Legendary, the direction slowly rotates around Y.
    ///
    /// Drives its own LineRenderer directly — no particle system or separate
    /// VFX component needed. The LineRenderer should be on this GameObject
    /// or a child; it's found automatically in Awake via GetComponentInChildren.
    ///
    /// LineRenderer setup on the prefab:
    ///   - Use World Space = true
    ///   - Positions Count = 2 (set by this script at runtime)
    ///   - Material: any Unlit/Color or URP Unlit material
    ///   - Width: leave at default — this script sets startWidth/endWidth each frame
    ///
    /// Pooled by LaserBeamZonePool — never Instantiate/Destroy this directly.
    /// </summary>
    public class LaserBeamZone : MonoBehaviour
    {
        private const float TickInterval        = 1f;
        private const float WeakenDurationPerTick = TickInterval * 1.5f;

        [Header("Visuals")]
        [Tooltip("Visual length of the beam in world units. Make this large enough " +
                 "to reach the arena boundary — the gameplay hitbox is infinite, " +
                 "so this only affects how far the beam looks.")]
        [SerializeField] private float visualLength = 40f;

        [Tooltip("Color of the beam while active.")]
        [SerializeField] private Color beamColor = new Color(0.4f, 0.8f, 1f, 1f);

        [System.NonSerialized] public LaserBeamZonePool Pool;

        private LineRenderer _line;

        private Vector3    _direction;
        private float      _beamWidth;
        private float      _damagePercentMaxHpPerSecond;
        private float      _weakenFraction;
        private float      _rotationDegreesPerSecond;
        private float      _remainingDuration;
        private float      _tickTimer;
        private bool       _isActive;
        private EnemyPool  _enemyPool;
        private Transform  _followTarget;

        private void Awake()
        {
            _line = GetComponentInChildren<LineRenderer>();
            if (_line)
            {
                _line.useWorldSpace  = true;
                _line.positionCount  = 2;
            }
            else
            {
                Debug.LogWarning("LaserBeamZone: no LineRenderer found on this GameObject or its children. " +
                                 "Add one to the prefab.", this);
            }
        }

        public void Begin(Transform followTarget, Vector3 direction, float beamWidth,
            float damagePercentMaxHpPerSecond, float weakenFraction,
            float durationSeconds, float rotationDegreesPerSecond, EnemyPool enemyPool)
        {
            _followTarget               = followTarget;
            _direction                  = direction.normalized;
            _beamWidth                  = beamWidth;
            _damagePercentMaxHpPerSecond = damagePercentMaxHpPerSecond;
            _weakenFraction             = weakenFraction;
            _rotationDegreesPerSecond   = rotationDegreesPerSecond;
            _remainingDuration          = durationSeconds;
            _tickTimer                  = 0f;
            _isActive                   = true;
            _enemyPool                  = enemyPool;

            UpdateTransformAndLine();
        }

        private Vector3 Origin => _followTarget ? _followTarget.position : transform.position;

        private void Update()
        {
            if (!_isActive) return;

            if (_rotationDegreesPerSecond != 0f)
                _direction = Quaternion.Euler(0f, _rotationDegreesPerSecond * Time.deltaTime, 0f) * _direction;

            UpdateTransformAndLine();

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

        private void UpdateTransformAndLine()
        {
            var origin = Origin;
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(_direction);

            if (!_line) return;

            var end = origin + _direction * visualLength;
            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
            _line.startWidth = _beamWidth;
            _line.endWidth   = _beamWidth * 0.5f; // taper toward the far end for visual quality
            _line.startColor = beamColor;
            _line.endColor   = new Color(beamColor.r, beamColor.g, beamColor.b, 0f); // fade to transparent
        }

        private void OnDisable()
        {
            _isActive = false;
            if (_line) _line.enabled = false;
        }

        private void OnEnable()
        {
            if (_line) _line.enabled = true;
        }

        private void DamageAndWeakenEnemiesInBeam()
        {
            foreach (var enemyView in _enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;
                if (!IsInsideBeam(enemyView.Data.Position)) continue;

                var damagePerTick = enemyView.Data.MaxHp * _damagePercentMaxHpPerSecond * TickInterval;
                enemyView.TakeDamage(damagePerTick);
                enemyView.DataRef.ApplyWeaken(_weakenFraction, WeakenDurationPerTick);
            }
        }

        private bool IsInsideBeam(Vector3 position)
        {
            var origin      = Origin;
            var toPosition  = position - origin;
            var forwardDist = Vector3.Dot(toPosition, _direction);
            if (forwardDist < 0f) return false;

            var closestPoint         = origin + _direction * forwardDist;
            var perpendicularDistance = Vector3.Distance(position, closestPoint);
            return perpendicularDistance <= _beamWidth;
        }

        private void Deactivate()
        {
            _isActive = false;
            Pool.Return(this);
        }
    }
}
