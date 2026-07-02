using Unity.Cinemachine;
using UnityEngine;

namespace Camera
{
    /// <summary>
    /// Calculates the minimum safe spawn inner radius at runtime — i.e. the
    /// distance from the arena centre beyond which enemies spawning are
    /// guaranteed to be off-screen, regardless of the current zoom level.
    ///
    /// WHY THIS EXISTS: WaveDirector.spawnInnerRadius was previously a hardcoded
    /// Inspector value that never updated when Cinemachine zoomed in or out.
    /// A hardcoded value that's safe at max zoom-out becomes too conservative
    /// at max zoom-in (wasting the spawn annulus by pushing enemies further
    /// than needed), and a value tuned for max zoom-in would pop enemies into
    /// view when zoomed out. This component solves both problems by reading
    /// the actual live camera state every time WaveDirector asks.
    ///
    /// HOW THE MATH WORKS (perspective camera, top-down):
    ///   The camera sits at some height H above the ground plane, angled
    ///   downward. At a given FieldOfView (vertical FOV), the half-height of
    ///   the visible frustum at ground level is:
    ///
    ///       halfHeight = H * tan(vFov / 2)
    ///
    ///   The half-diagonal (the screen corner, worst case) is:
    ///
    ///       halfDiagonal = halfHeight * sqrt(1 + aspectRatio²)
    ///
    ///   Adding a buffer margin (bufferFraction, e.g. 0.15 = 15%) ensures that
    ///   fast player movement, a zoom-out event, or a brief camera shake won't
    ///   momentarily reveal a just-spawned enemy at the ring's inner edge.
    ///
    /// Attach to: the same GameObject as CinemachinePlayerCamera.
    /// Wire: assign the scene's main Camera (not the CinemachineCamera virtual
    ///       camera) to sceneCamera — that's what holds the real FOV and aspect.
    /// </summary>
    public class SpawnRadiusProvider : MonoBehaviour
    {
        [Header("Camera reference")]
        [Tooltip("The scene's Main Camera (the one with CinemachineBrain), NOT the CinemachineCamera " +
                 "virtual camera. This is what holds the real runtime FieldOfView and aspect ratio.")]
        [SerializeField] private UnityEngine.Camera sceneCamera;

        [Header("Ground plane")]
        [Tooltip("World Y of the arena floor. Used to compute camera-to-ground distance. " +
                 "If your floor is at Y=0, leave this at 0.")]
        [SerializeField] private float groundY = 0f;

        [Header("Safety margin")]
        [Tooltip("Fractional buffer added on top of the calculated half-diagonal. " +
                 "0.15 = 15% extra so fast movement or a zoom-out event don't briefly " +
                 "reveal a spawn at the inner ring edge. Tune upward if pop-in ever occurs.")]
        [SerializeField][Range(0f, 0.5f)] private float bufferFraction = 0.15f;

        /// <summary>
        /// The minimum safe inner radius for the spawn annulus at the current
        /// camera FOV and position. Call this from WaveDirector each time it
        /// rolls a spawn position — cheap enough to call every spawn, no caching
        /// needed since it's just a handful of float ops per call.
        /// </summary>
        public float SafeInnerRadius
        {
            get
            {
                if (!sceneCamera)
                {
                    Debug.LogWarning("SpawnRadiusProvider: sceneCamera not assigned — " +
                                     "returning fallback radius of 12. Assign the scene's " +
                                     "Main Camera in the Inspector.", this);
                    return 12f;
                }

                // Distance from camera to the ground plane along the world Y axis.
                // Works for any camera height, including if the camera bobs or the
                // ground is not at Y=0 (set groundY to match your floor height).
                var camHeight = Mathf.Abs(sceneCamera.transform.position.y - groundY);

                // Vertical FOV in radians — sceneCamera.fieldOfView is already the
                // vertical FOV in degrees (Unity convention for perspective cameras).
                var vFovRad = sceneCamera.fieldOfView * Mathf.Deg2Rad;

                // Half-height of the visible frustum at ground level.
                var halfHeight = camHeight * Mathf.Tan(vFovRad * 0.5f);

                // Half-diagonal: the screen corner is the farthest visible point
                // from centre, so this is the worst-case "is this point on screen" distance.
                var aspect = sceneCamera.aspect;
                var halfDiagonal = halfHeight * Mathf.Sqrt(1f + aspect * aspect);

                // Apply safety buffer and return.
                return halfDiagonal * (1f + bufferFraction);
            }
        }

        /// <summary>
        /// Editor-visible readout of the current safe radius — useful for tuning
        /// bufferFraction in Play mode without needing to add debug logs.
        /// Read-only; has no effect on runtime behaviour.
        /// </summary>
        [Header("Runtime readout (read-only)")]
        [SerializeField][HideInInspector] private float _debugCurrentRadius;

        private void Update()
        {
#if UNITY_EDITOR
            // Keep the debug field live in the Inspector during Play mode so you
            // can watch it change as the camera zooms in and out.
            _debugCurrentRadius = SafeInnerRadius;
#endif
        }
    }
}
