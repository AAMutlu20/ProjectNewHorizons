using UnityEngine;

namespace Player
{
    /// <summary>
    /// Minimal player movement for the graybox phase.
    /// Uses Rigidbody2D.MovePosition — no direct transform manipulation,
    /// so it respects collision boundaries without triggering triggers.
    ///
    /// Attach to: PlayerRoot in the [Player] hierarchy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Mobile — assign a virtual joystick here")]
        [SerializeField] private bool useMobileInput;
        // If you add a virtual joystick asset later, expose it here:
        // [SerializeField] private Joystick virtualJoystick;

        private Rigidbody2D _rb;
        private Vector2 _input;

        // Expose position for WaveManager to read player location
        public Vector2 Position => _rb.position;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // Keyboard / gamepad input
            if (!useMobileInput)
            {
                _input = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                ).normalized;
            }
            // else: read from virtual joystick asset
            // _input = new Vector2(virtualJoystick.Horizontal, virtualJoystick.Vertical).normalized;
        }

        private void FixedUpdate()
        {
            _rb.MovePosition(_rb.position + _input * (moveSpeed * Time.fixedDeltaTime));
        }
    }
}
