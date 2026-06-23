using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Resolves a candidate X/Z spawn position into an actually-valid world
    /// position: raycasts down to find the REAL ground height at that point
    /// (so a raised platform, a ramp, or the floor mesh's own noise are all
    /// respected, not assumed flat), then checks for obstruction (so an enemy
    /// never spawns inside a wall, a well, or another solid collider).
    ///
    /// groundOffsetY (from EnemyTypeSo) is correctly treated as a LOCAL
    /// foot-to-pivot offset, applied on top of whatever ground height this
    /// resolver actually finds -- not a hardcoded absolute world Y.
    ///
    /// Attach to: the same GameObject as WaveDirector, or any [Systems] object --
    /// EnemyPool holds a reference to this and calls TryResolve per spawn.
    /// </summary>
    public class SpawnPositionResolver : MonoBehaviour
    {
        [Header("Ground detection")]
        [Tooltip("Layers considered 'ground' for the downward raycast -- the floor, platforms, ramps, etc.")]
        [SerializeField] private LayerMask groundLayers;

        [Tooltip("Raycast starts this far above the candidate position, so it can find ground " +
                 "even on a platform raised above the nominal arena Y.")]
        [SerializeField] private float raycastStartHeight = 50f;

        [Tooltip("Raycast travels this far downward looking for ground. Should comfortably exceed " +
                 "the tallest expected platform/structure, plus raycastStartHeight's own margin.")]
        [SerializeField] private float raycastMaxDistance = 200f;

        [Header("Obstruction check")]
        [Tooltip("Layers that should block a spawn if something is already there -- the Obstructions layer, props, other solid geometry.")]
        [SerializeField] private LayerMask obstructionLayers;

        [Tooltip("Radius of the overlap check used to detect obstruction, roughly matching an enemy's footprint.")]
        [SerializeField] private float obstructionCheckRadius = 0.6f;

        [Tooltip("How many alternate candidate positions to try (via retryPositionFunc) before giving up on this spawn.")]
        [SerializeField] private int maxRetries = 5;

        /// <summary>
        /// Attempts to resolve candidateXz (Y ignored) into a valid grounded,
        /// unobstructed world position at groundOffsetY above the real
        /// detected ground. If the first candidate is obstructed, calls
        /// retryPositionFunc to get a new candidate and tries again, up to
        /// maxRetries times. Returns false if no valid position was found.
        /// </summary>
        public bool TryResolve(Vector3 candidateXz, float groundOffsetY,
            System.Func<Vector3> retryPositionFunc, out Vector3 resolvedPosition)
        {
            var candidate = candidateXz;

            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (TryFindGroundHeight(candidate, out var groundHeight))
                {
                    var groundedPosition = new Vector3(candidate.x, groundHeight + groundOffsetY, candidate.z);

                    if (!IsObstructed(groundedPosition))
                    {
                        resolvedPosition = groundedPosition;
                        return true;
                    }
                }

                candidate = retryPositionFunc();
            }

            resolvedPosition = default;
            return false;
        }

        private bool TryFindGroundHeight(Vector3 candidateXz, out float groundHeight)
        {
            var rayOrigin = new Vector3(candidateXz.x, raycastStartHeight, candidateXz.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, raycastMaxDistance, groundLayers))
            {
                groundHeight = hit.point.y;
                return true;
            }

            groundHeight = default;
            Debug.LogWarning($"SpawnPositionResolver: no ground found below ({candidateXz.x:F1}, {candidateXz.z:F1}) -- " +
                              "check groundLayers includes the floor, and raycastMaxDistance is tall enough.", this);
            return false;
        }

        private bool IsObstructed(Vector3 position)
        {
            return Physics.CheckSphere(position, obstructionCheckRadius, obstructionLayers);
        }
    }
}
