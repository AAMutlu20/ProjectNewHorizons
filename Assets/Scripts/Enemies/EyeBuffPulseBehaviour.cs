using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Eye ability: periodically pulses a buff (damage/speed multiplier) and
    /// a flat heal to nearby enemies, per the design doc's "buffs enemies"
    /// behaviour. Runs via IPeriodicAbility — independent of attack range,
    /// through the existing managed update loop (no per-enemy Update()).
    ///
    /// Attach to: the EyeWinged prefab, alongside BehaviourController.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    [RequireComponent(typeof(EnemyView))]
    public class EyeBuffPulseBehaviour : MonoBehaviour, IPeriodicAbility
    {
        [Header("Pulse timing")]
        [SerializeField] private float pulseInterval = 6f;
        [SerializeField] private float pulseRadius = 8f;

        [Header("Buff")]
        [SerializeField] private float buffMultiplier = 1.25f;
        [SerializeField] private float buffDuration = 5f;
        [SerializeField] private float healAmount = 15f;

        private BehaviourController _behaviour;
        private EnemyView _view;

        // Reused per-pulse — avoids allocation in the hot path
        private readonly List<int> _nearbyIndices = new(16);

        private float _pulseTimer;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
            _view = GetComponent<EnemyView>();
            _pulseTimer = pulseInterval;
        }

        public void TickAbility(ref EnemyData enemy, float deltaTime)
        {
            _pulseTimer -= deltaTime;
            if (_pulseTimer > 0f) return;

            _pulseTimer = pulseInterval;
            PulseBuffToNearbyEnemies(enemy.Position);
        }

        private void PulseBuffToNearbyEnemies(Vector3 pulseOrigin)
        {
            if (!_behaviour.Grid || _behaviour.ActiveEnemies == null) return;

            _nearbyIndices.Clear();
            _behaviour.Grid.GetNeighbourIndices(pulseOrigin, pulseRadius, _nearbyIndices);

            foreach (var index in _nearbyIndices)
            {
                if (index < 0 || index >= _behaviour.ActiveEnemies.Count) continue;

                var target = _behaviour.ActiveEnemies[index];
                if (target == _view || !target.Data.IsAlive) continue;

                BuffAndHeal(target);
            }
        }

        private void BuffAndHeal(EnemyView target)
        {
            ref var targetData = ref target.DataRef;

            targetData.ApplyBuff(buffMultiplier, buffDuration);
            targetData.Hp = Mathf.Min(targetData.MaxHp, targetData.Hp + healAmount);

            EventBus.Emit(new EnemyBuffedEvent
            {
                Target = target,
                Position = targetData.Position,
                Duration = buffDuration,
            });
        }
    }

    /// <summary>Emitted when an enemy receives a buff pulse — for the glow/VFX system to react to.</summary>
    public struct EnemyBuffedEvent
    {
        public EnemyView Target;
        public Vector3 Position;
        public float Duration;
    }
}
