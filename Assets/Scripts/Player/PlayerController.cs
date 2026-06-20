using Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Player movement. Speed is (base + flat Movement Speed stat) scaled by
    /// the strongest active slow — see the design doc's movement formula.
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
    [RequireComponent(typeof(StatSheet))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 6f;

        [Header("Input")]
        [Tooltip("Drag the Move action from PlayerInputActions.inputactions here.")]
        [SerializeField] private InputActionReference moveAction;

        [Header("Mobile — assign a virtual joystick here")]
        [SerializeField] private bool useMobileInput;
        // If you add a virtual joystick UI later, read its Vector2 output here instead:
        // [SerializeField] private Joystick virtualJoystick;

        private Rigidbody _rb;
        private StatSheet _statSheet;
        private Vector2 _input; // x = horizontal (world X), y = vertical (world Z)

        // Highest active slow as a fraction (0.5 = 50% slowed). Set by status
        // effect systems (webs, ground hazards, debuffs) — defaults to none.
        private float _strongestSlowFraction;

        // Expose position for other systems to read player location
        public Vector3 Position => _rb.position;

        public float CurrentMoveSpeed =>
            (baseMoveSpeed + _statSheet.GetTotal(StatType.MovementSpeed)) * (1f - _strongestSlowFraction);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _statSheet = GetComponent<StatSheet>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ
                             | RigidbodyConstraints.FreezePositionY;
        }

        private void OnEnable()
        {
            if (moveAction) moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction) moveAction.action.Disable();
        }

        private void Update()
        {
            if (useMobileInput) return;
            _input = moveAction
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            // Composite 2D Vector bindings already normalize diagonals, but clamp
            // magnitude defensively in case of overlapping/raw device input.
            if (_input.sqrMagnitude > 1f) _input.Normalize();
            // else: read from virtual joystick UI
            // _input = virtualJoystick.Value;
        }

        private void FixedUpdate()
        {
            // Map 2D input to XZ plane — Y (height) is untouched by movement
            var moveDirection = new Vector3(_input.x, 0f, _input.y);
            _rb.MovePosition(_rb.position + moveDirection * (CurrentMoveSpeed * Time.fixedDeltaTime));
        }

        /// <summary>Called by status-effect systems to apply or clear a slow. Pass 0 to clear.</summary>
        public void SetStrongestSlow(float slowFraction)
        {
            _strongestSlowFraction = Mathf.Clamp01(slowFraction);
        }
    }
}
