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

        private void Start()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(ShakeOnHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(ShakeOnHit);
        }

        private void ShakeOnHit(PlayerHealthChangedEvent @event)
        {
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
