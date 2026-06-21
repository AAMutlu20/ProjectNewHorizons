using Unity.Cinemachine;
using UnityEngine;

namespace Camera
{
    /// <summary>
    /// Replaces the old hand-rolled CameraFollow.cs. Position/follow smoothing
    /// and arena-edge confinement are NOT done here -- they're handled natively
    /// by Cinemachine's own pipeline (CinemachineCamera's Tracking Target +
    /// a CinemachinePositionComposer for follow damping, plus a
    /// CinemachineConfiner3D extension for the arena bounds). This script's
    /// only job is exposing a clean runtime zoom API, since "zoom in/out" is
    /// gameplay-driven (player input, boss-fight framing) and needs a script
    /// entry point Cinemachine doesn't provide out of the box.
    ///
    /// Scene setup required (not done by this script):
    ///   1. A GameObject with CinemachineCamera, Tracking Target = PlayerRoot,
    ///      a CinemachinePositionComposer for follow damping.
    ///   2. A CinemachineConfiner3D extension on that same object, with
    ///      BoundingVolume set to a Collider surrounding the playable arena.
    ///   3. A CinemachineBrain on the actual scene Camera (not the CinemachineCamera).
    ///   4. The Unity Camera's own clear flags / render settings as usual.
    ///
    /// Attach to: the GameObject holding the CinemachineCamera component.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachinePlayerCamera : MonoBehaviour
    {
        [Header("Zoom (drives Lens.FieldOfView for a perspective camera)")]
        [SerializeField] private float minFieldOfView = 25f;
        [SerializeField] private float maxFieldOfView = 60f;
        [SerializeField] private float defaultFieldOfView = 40f;
        [SerializeField] private float zoomSmoothing = 6f;

        private CinemachineCamera _cinemachineCamera;
        private float _targetFieldOfView;

        /// <summary>0 = fully zoomed in (minFieldOfView), 1 = fully zoomed out (maxFieldOfView).</summary>
        public float CurrentZoom01 =>
            maxFieldOfView > minFieldOfView
                ? Mathf.InverseLerp(minFieldOfView, maxFieldOfView, _cinemachineCamera.Lens.FieldOfView)
                : 0f;

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _targetFieldOfView = defaultFieldOfView;

            var lens = _cinemachineCamera.Lens;
            lens.FieldOfView = defaultFieldOfView;
            _cinemachineCamera.Lens = lens;
        }

        private void Update()
        {
            var lens = _cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFieldOfView, zoomSmoothing * Time.deltaTime);
            _cinemachineCamera.Lens = lens;
        }

        /// <summary>Sets the target zoom directly, 0 (closest) to 1 (farthest). Smoothly interpolates toward it over time.</summary>
        public void SetZoom01(float zoom01)
        {
            zoom01 = Mathf.Clamp01(zoom01);
            _targetFieldOfView = Mathf.Lerp(minFieldOfView, maxFieldOfView, zoom01);
        }

        /// <summary>Nudges the zoom by a delta, e.g. from a scroll-wheel or pinch gesture. Positive = zoom out.</summary>
        public void AdjustZoom(float delta01)
        {
            SetZoom01(CurrentZoom01 + delta01);
        }
    }
}
