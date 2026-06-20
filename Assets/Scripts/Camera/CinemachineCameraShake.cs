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
        [SerializeField] private Vector3 debugShakeVelocity = Vector3.up;

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
            ShakeAtPosition(transform.position, debugShakeVelocity);
        }

        [ContextMenu("Debug: Shake")]
        private void DebugShake()
        {
            Shake(debugShakeVelocity);
        }
    }
}
