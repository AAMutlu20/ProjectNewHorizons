using Core;
using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Camera
{
    /// <summary>
    /// Triggers Cinemachine impulse shakes from gameplay code — e.g. on player
    /// hit, boss ground-slam impact, or melee swing connect.
    /// </summary>
    public class CinemachineCameraShake : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Debug")]
        [SerializeField] private Vector3 ShakeVelocity = Vector3.up;

        private float _lastKnownHp = -1f; // -1 sentinel: no reading yet, so the first event never counts as a "decrease"

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(ShakeOnHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(ShakeOnHit);
        }

        private void ShakeOnHit(PlayerHealthChangedEvent @event)
        {
            // PlayerHealthChangedEvent also fires from Heal() and passive regen,
            // not just actual damage -- Heal() ticks every frame while below max
            // HP, which was generating a fresh impulse every single frame and
            // never letting the previous one decay. Only an actual HP decrease
            // should shake the camera.
            var isDecrease = _lastKnownHp >= 0f && @event.Current < _lastKnownHp;
            _lastKnownHp = @event.Current;

            if (!isDecrease) return;

            Shake(ShakeVelocity);
        }

        private void Shake(Vector3 velocity)
        {
            impulseSource.GenerateImpulseWithVelocity(velocity);
        }

        private void ShakeAtPosition(Vector3 position, Vector3 velocity)
        {
            impulseSource.GenerateImpulseAtPositionWithVelocity(position, velocity);
        }

        [ContextMenu("Debug: Shake At This Position")]
        private void DebugShakeAtThisPosition()
        {
            ShakeAtPosition(transform.position, ShakeVelocity);
        }

        [ContextMenu("Debug: Shake")]
        private void DebugShake()
        {
            Shake(ShakeVelocity);
        }
    }
}
