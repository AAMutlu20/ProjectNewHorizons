using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Minimal player movement for the graybox phase.
    /// Uses Rigidbody.MovePosition on the XZ plane — no direct transform manipulation,
    /// so it respects collision boundaries without bypassing physics.
    ///
    /// Reads movement through the new Input System (InputActionReference) rather than
    /// the legacy UnityEngine.Input class — required once Player Settings → Active Input
    /// Handling is set to "Input System Package" or "Both".
    ///
    /// Attach to: PlayerRoot in the [Player] hierarchy.
    /// Wire: drag the "Move" action from PlayerInputActions.inputactions into moveAction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Input")]
        [Tooltip("Drag the Move action from PlayerInputActions.inputactions here.")]
        [SerializeField] private InputActionReference moveAction;

        [Header("Mobile — assign a virtual joystick here")]
        [SerializeField] private bool useMobileInput;
        // If you add a virtual joystick UI later, read its Vector2 output here instead:
        // [SerializeField] private Joystick virtualJoystick;

        private Rigidbody _rb;
        private Vector2   _input; // x = horizontal (world X), y = vertical (world Z)

        // Expose position for other systems to read player location
        public Vector3 Position => _rb.position;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ
                             | RigidbodyConstraints.FreezePositionY;
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
        }

        private void Update()
        {
            if (!useMobileInput)
            {
                _input = moveAction != null
                    ? moveAction.action.ReadValue<Vector2>()
                    : Vector2.zero;

                // Composite 2D Vector bindings already normalize diagonals, but clamp
                // magnitude defensively in case of overlapping/raw device input.
                if (_input.sqrMagnitude > 1f) _input.Normalize();
            }
            // else: read from virtual joystick UI
            // _input = virtualJoystick.Value;
        }

        private void FixedUpdate()
        {
            // Map 2D input to XZ plane — Y (height) is untouched by movement
            var moveDir = new Vector3(_input.x, 0f, _input.y);
            _rb.MovePosition(_rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime));
        }
    }
}
