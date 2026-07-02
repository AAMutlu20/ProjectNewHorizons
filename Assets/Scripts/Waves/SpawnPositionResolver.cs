using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Resolves a candidate XZ position into a valid world spawn point.
    ///
    /// TWO RULES ONLY:
    ///   1. A downward raycast must hit a collider on groundLayers.
    ///      The hit point becomes the enemy's Y position (+ groundOffsetY from the SO).
    ///   2. The candidate must be within the spawn ring (enforced by WaveDirector
    ///      before calling — this resolver does not re-check distance).
    ///
    /// The obstruction check has been removed. It was hitting unidentified
    /// colliders in the scene and rejecting every valid position. If an enemy
    /// spawns inside a prop, move the prop — do not add obstruction complexity
    /// back until the scene is fully built and layers are confirmed stable.
    ///
    /// Attach to: the same GameObject as WaveDirector.
    /// Wire: groundLayers to the Ground layer mask.
    /// </summary>
    public class SpawnPositionResolver : MonoBehaviour
    {
        [Header("Ground detection")]
        [Tooltip("Layers the downward raycast looks for. Must include your floor tile layer (Ground).")]
        [SerializeField] private LayerMask groundLayers;

        [Tooltip("Raycast origin is this many world units ABOVE the candidate XZ position. " +
                 "Must be higher than your highest floor tile's world Y.")]
        [SerializeField] private float raycastStartHeight = 50f;

        [Tooltip("How far downward the ray travels. Must reach your lowest floor Y.")]
        [SerializeField] private float raycastMaxDistance = 200f;

        [Tooltip("How many times to re-roll a new candidate position if the first has no ground beneath it. " +
                 "With the obstruction check removed, retries only matter for positions that fall " +
                 "outside the floor geometry (e.g. over a hole or beyond the arena edge).")]
        [SerializeField] private int maxRetries = 5;

        /// <summary>
        /// Fires a downward raycast at candidatePos (Y is ignored — ray always starts
        /// at raycastStartHeight). Returns true if ground was found and sets
        /// resolvedPosition to the hit point + groundOffsetY.
        ///
        /// If no ground is found, calls retryPositionFunc for a new candidate
        /// and tries again up to maxRetries times.
        /// </summary>
        public bool TryResolve(Vector3 candidatePos, float groundOffsetY,
            System.Func<Vector3> retryPositionFunc, out Vector3 resolvedPosition)
        {
            var candidate = candidatePos;

            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                // Always cast from a fixed height above the candidate XZ —
                // the Y value of candidate is irrelevant (player Y, world Y, whatever).
                var rayOrigin = new Vector3(candidate.x, raycastStartHeight, candidate.z);

                if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, raycastMaxDistance, groundLayers))
                {
                    resolvedPosition = new Vector3(candidate.x, hit.point.y + groundOffsetY, candidate.z);
                    return true;
                }

                // No ground under this candidate — roll a new one and try again.
                candidate = retryPositionFunc();
            }

            // All attempts found no ground. Likely means the spawn ring extends
            // beyond the floor geometry. Increase floor coverage or reduce spawn radius.
            Debug.LogWarning(
                $"SpawnPositionResolver: no ground found after {maxRetries + 1} attempts. " +
                $"Last candidate: ({candidate.x:F1}, {candidate.z:F1}). " +
                "Check that groundLayers includes your floor layer and that " +
                "the spawn ring radius does not extend beyond the floor geometry.",
                this);

            resolvedPosition = default;
            return false;
        }
    }
}
