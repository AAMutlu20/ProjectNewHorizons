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
    /// External forces (e.g. TornadoGhost's pull) go through ApplyExternalPull
    /// rather than calling MovePosition themselves — combining both into one
    /// MovePosition call per frame avoids the two competing depending on
    /// script execution order.
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
    [RequireComponent(typeof(SlowEffectController))]
    public class PlayerController : MonoBehaviour
    {
        private const float ExternalPullDecayRate = 4f;

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
        private SlowEffectController _slowEffects;
        private Vector2 _input; // x = horizontal (world X), y = vertical (world Z)

        // Accumulated external displacement per second (e.g. TornadoGhost's pull).
        // Decays toward zero so a pull feels like a force, not a teleport.
        private Vector3 _externalPullVelocity;

        // Holds the last non-zero input direction — used by abilities that need
        // a "facing" (e.g. Cone of Fire), since the player has no rotation of
        // their own. Defaults to forward so abilities always get a valid,
        // non-zero direction even before the player has ever moved.
        private Vector3 _facingDirection = Vector3.forward;

        // Expose position for other systems to read player location
        public Vector3 Position => _rb.position;

        /// <summary>
        /// The direction the player was last walking, held constant while
        /// standing still. Not a true facing/aim direction — there's no
        /// rotation or aim input in this game — but the closest available
        /// substitute for "in front of the player" (e.g. Cone of Fire).
        /// </summary>
        public Vector3 FacingDirection => _facingDirection;

        public float CurrentMoveSpeed =>
            (baseMoveSpeed + _statSheet.GetTotal(StatType.MovementSpeed)) * (1f - _slowEffects.StrongestSlowFraction);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _statSheet = GetComponent<StatSheet>();
            _slowEffects = GetComponent<SlowEffectController>();

            // Kinematic, matching the enemy revert -- the player's own movement
            // is entirely MovePosition-driven (FixedUpdate below), same as every
            // enemy via BehaviourController. Previously the player rigidbody
            // relied on the Inspector's default (non-kinematic) while colliding
            // against now-non-kinematic enemies -- when the player walked into
            // an enemy, the solver's penetration-resolution impulse pushed/spun
            // the player unpredictably, fighting the input-driven MovePosition
            // call every tick. Kinematic removes the player from that fight
            // entirely: no solver-applied impulses, ever, only what FixedUpdate
            // explicitly tells it to do.
            _rb.isKinematic = true;
            _rb.useGravity = false;

            _rb.constraints = RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ;
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

            UpdateFacingDirection();
        }

        private void UpdateFacingDirection()
        {
            if (_input.sqrMagnitude <= 0.0001f) return; // standing still — keep the last facing direction

            _facingDirection = new Vector3(_input.x, 0f, _input.y).normalized;
        }

        private void FixedUpdate()
        {
            // Map 2D input to XZ plane — Y (height) is untouched by movement
            var inputDirection = new Vector3(_input.x, 0f, _input.y);
            var inputDisplacement = inputDirection * (CurrentMoveSpeed * Time.fixedDeltaTime);
            var pullDisplacement = _externalPullVelocity * Time.fixedDeltaTime;

            _rb.MovePosition(_rb.position + inputDisplacement + pullDisplacement);

            DecayExternalPull();
        }

        /// <summary>
        /// Applies an external displacement force (e.g. being pulled toward a
        /// TornadoGhost). Additive with any existing pull and with player
        /// input — does not override or pause normal movement.
        /// </summary>
        public void ApplyExternalPull(Vector3 pullVelocity)
        {
            _externalPullVelocity += pullVelocity;
        }

        private void DecayExternalPull()
        {
            _externalPullVelocity = Vector3.Lerp(
                _externalPullVelocity, Vector3.zero, Time.fixedDeltaTime * ExternalPullDecayRate);
        }
    }
}
