using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Drives the visual for one LaserBeamZone instance.
    /// LaserBeamZone already updates its own transform position and rotation
    /// to follow the player and match the beam direction every frame — this
    /// script just reads that transform and feeds it into a LineRenderer
    /// (or optionally a ParticleSystem) so the beam is actually visible.
    ///
    /// TWO VISUAL OPTIONS — use whichever fits your art style:
    ///
    ///   LINE RENDERER (recommended for a clean energy beam look):
    ///     Add a LineRenderer component to the LaserBeamZone prefab.
    ///     Set its positions to [0, 0, 0] and [0, 0, beamLength] in local space.
    ///     Wire it to the lineRenderer field below.
    ///     This script sets point[0] to the local origin and point[1] along
    ///     +Z (which is the beam's forward direction after LookRotation).
    ///     Set LineRenderer → Use World Space = false so it follows the transform.
    ///
    ///   PARTICLE SYSTEM (for a more organic/magical look):
    ///     Add a ParticleSystem child to the LaserBeamZone prefab.
    ///     Set it to emit along +Z in local space.
    ///     Wire it to the beamParticles field below.
    ///     The particle system follows the transform automatically since it's a child.
    ///
    /// Attach to: the LaserBeamZone PREFAB root (not a scene object).
    /// The component activates/deactivates with the pooled GameObject automatically.
    /// </summary>
    public class LaserBeamVfx : MonoBehaviour
    {
        [Header("Option A — Line Renderer")]
        [Tooltip("Assign a LineRenderer on this GameObject. " +
                 "Set Use World Space = false, positions = {(0,0,0), (0,0,1)}.")]
        [SerializeField] private LineRenderer lineRenderer;

        [Tooltip("How far the line renderer extends in the beam's forward direction. " +
                 "Set large enough to reach the arena boundary — the beam is infinite " +
                 "for gameplay purposes, so make this long enough to look infinite visually.")]
        [SerializeField] private float beamVisualLength = 200f;

        [Header("Option B — Particle System")]
        [Tooltip("Assign a ParticleSystem child if using particles instead of a line renderer. " +
                 "It will play/stop automatically with this component's enable/disable.")]
        [SerializeField] private ParticleSystem beamParticles;

        [Header("Width")]
        [Tooltip("Visual width of the beam. Should roughly match LaserBeamAbility.BeamWidth " +
                 "so the visual matches the gameplay hitbox.")]
        [SerializeField] private float beamWidth = 1.5f;

        private void OnEnable()
        {
            if (lineRenderer)
            {
                lineRenderer.enabled = true;
                lineRenderer.startWidth = beamWidth;
                lineRenderer.endWidth = beamWidth;
                // Points in local space — (0,0,0) is the origin, (0,0,beamLength) extends forward.
                // The transform's LookRotation (set by LaserBeamZone.UpdateTransform) makes
                // local +Z align with the beam direction, so no manual direction math needed here.
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.forward * beamVisualLength);
            }

            if (beamParticles)
                beamParticles.Play();
        }

        private void OnDisable()
        {
            if (lineRenderer)
                lineRenderer.enabled = false;

            if (beamParticles)
                beamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
