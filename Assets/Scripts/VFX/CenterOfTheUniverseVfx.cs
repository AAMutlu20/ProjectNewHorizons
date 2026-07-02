using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Scales a persistent visual (particle system, mesh, or shader sphere)
    /// to match CenterOfTheUniverseController's live radius every frame.
    /// The controller already moves this object above the player — this script
    /// only handles the visual scale.
    ///
    /// Attach to: the same GameObject as CenterOfTheUniverseController.
    /// Wire: controller (auto-fetched from same object), visualTransform
    ///       (the child containing the black hole mesh/particles to scale).
    /// </summary>
    public class CenterOfTheUniverseVfx : MonoBehaviour
    {
        [SerializeField] private Player.CenterOfTheUniverseController controller;
        [Tooltip("The Transform whose localScale to drive. Can be this object or a child.")]
        [SerializeField] private Transform visualTransform;

        [Tooltip("The visual's authored radius at localScale = 1, used to derive the correct scale factor.")]
        [SerializeField] private float visualBaseRadius = 1f;

        private void Awake()
        {
            if (!controller) controller = GetComponent<Player.CenterOfTheUniverseController>();
            if (!visualTransform) visualTransform = transform;
            Debug.Assert(controller, "CenterOfTheUniverseVfx: controller not found.", this);
        }

        private void Update()
        {
            if (!controller.IsGranted) return;

            var scaleFactor = visualBaseRadius > 0f
                ? controller.CurrentRadius / visualBaseRadius
                : controller.CurrentRadius;

            visualTransform.localScale = Vector3.one * scaleFactor;
        }
    }
}
