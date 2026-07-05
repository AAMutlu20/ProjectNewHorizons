using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Core
{
    /// <summary>
    /// Warms up a previously-traced GraphicsStateCollection (.graphicsstate asset,
    /// captured via PsoTraceRecorder) while the loading screen is showing --
    /// hiding the PSO compilation cost behind the fake progress bar instead of
    /// letting it stutter mid-gameplay the first time each shader/material
    /// combination actually renders.
    ///
    /// Attach to: the same GameObject as MainMenuLoadingController, in the
    /// MainMenu scene.
    /// </summary>
    public class PsoWarmupController : MonoBehaviour
    {
        [Tooltip("Drag the .graphicsstate asset here once PsoTraceRecorder has captured one.")]
        [SerializeField] private GraphicsStateCollection tracedStates;

        /// <summary>
        /// Call this from MainMenuLoadingController.StartGame(), alongside starting
        /// the async scene load, so warmup runs in parallel with it.
        /// </summary>
        public void BeginWarmup()
        {
            if (tracedStates == null)
            {
                Debug.LogWarning("[PsoWarmupController] No traced GraphicsStateCollection assigned -- skipping warmup.");
                return;
            }

            // Synchronous: blocks until all PSOs in the collection are created.
            // Safe to call here since it's already hidden behind the loading screen.
            var handle = tracedStates.WarmUp();
            handle.Complete();

            Debug.Log("[PsoWarmupController] PSO warmup complete.");
        }
    }
}
