using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float groundAcceleration = 0.85f;
    public float groundMaxVelocity = 7.5f;                  // groundMaxVelocity * groundFriction gets added on top during Start() to ensure that the player can reach groundMaxVelocity.
    public float groundMinVelocity = 0.5f;                  // The minimum velocity on the ground allowed before velocity gets set to 0.
    public float groundFriction = 0.1f;

    public float airAcceleration = 1.0f;
    public float airMaxVelocity = 1.0f;                     // airMaxVelocity * airFriction gets added on top during Start() to ensure that the player can reach airMaxVelocity.
    public float airMinVelocity = 0.5f;                     // The minimum velocity in the air allowed before horizontal velocity gets set to 0.
    public float airFriction = 0.1f;

    public float gravity = 0.5f;

    public float jumpForce = 8.0f;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode strafeForwardKey = KeyCode.W;
    public KeyCode strafeLeftKey = KeyCode.A;
    public KeyCode strafeBackwardKey = KeyCode.S;
    public KeyCode strafeRightKey = KeyCode.D;

    private KeyCode _jumpKey;

    [Header("Ground Check")]
    public float playerHeight = 2.0f;
    public float maxVerticalVelocityToBeAirborne = 8.0f;    // The maximum vertical velocity allowed before the player is considered airborne.
    public LayerMask whatIsGround;
    public bool grounded = false;
    public bool addUpwardVerticalMomentumOnJump = false;    // Should the jump velocity be affected by the player's current upward momentum? (Not implemented yet)
    public bool addDownwardVerticalMomentumOnJump = false;  // Should the jump velocity be affected by the player's current downward momentum? (Not implemented yet)


    [Header("Slope Check")]
    public float maxSlopeAngle = 40.0f;                     // The maximum angle allowed before the player is considered airborne.
    public Transform orientation;                           // Orientation of the camera/player model.

    private RaycastHit _slopeHit;                            // Structure used to get info back from the boxCast in IsGround().

    private float _horizontalInput;                          // left = -1, right = 1.
    private float _verticalInput;                            // down = -1, up = 1.

    private Vector3 _wishDir;                                // The direction the player wants to go (and normal to the slope if there is one).
    private Vector3 _wishDirGroundAccel;                     // The ground acceleration in the _wishDir direction.
    private Vector3 _wishDirAirAccel;                        // The air acceleration in the _wishDir direction.

    private float currentWishDirVel;                        // Current velocity in the WishDir direction.

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        groundMaxVelocity += groundMaxVelocity * groundFriction;
        airMaxVelocity += airMaxVelocity * airFriction;
        _jumpKey = jumpKey;
    }

    private void FixedUpdate()
    {
        Debug.Log("Velocity\t\t: " + rb.velocity.magnitude + "\n\nWishDir Velocity\t: " + currentWishDirVel +
            "\n\nHorizontal Velocity\t: " + new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).magnitude +
            "\n\nVertical Velocity\t: " + new Vector3(0.0f, rb.velocity.y, 0.0f).magnitude);

        IsGrounded();

        MyInput();

        // Get the direction the player wants to go (and normal to the slope if there is one).
        GetWishDir();

        // Get the current velocity in the WishDir direction.
        GetCurrentWishDirVel();

        HandleGroundAccel();

        HandleAirAccel();

        HandleGroundFriction();

        HandleAirFriction();

        HandleGravity();

        MovePlayer();
    }

    private bool IsGrounded()
    {
        // Perform a box check from the bottom of the player that is 25.5% the height of the player.
        // Determine if the collided object is a ground and report collision info to slopeHit.
        bool groundCheck = Physics.BoxCast(transform.position, transform.localScale * 0.5f, Vector3.down, out _slopeHit, transform.rotation, playerHeight * 0.255f, whatIsGround);

        if (_slopeHit.normal == new Vector3(0.0f, 0.0f, 0.0f))
            _slopeHit.normal = new Vector3(0.0f, 1.0f, 0.0f);

        // Player is considered grounded if their velocity is below maxVerticalVelocityToBeAirborne(the maximum vertical velocity allowed before the player is considered airborne),
        // touching a ground(groundCheck), and the ground has an angle less than maxSlopeAngle.
        if (rb.velocity.y < maxVerticalVelocityToBeAirborne && groundCheck && Vector3.Angle(Vector3.up, _slopeHit.normal) < maxSlopeAngle)
            grounded = true;
        else
        {
            grounded = false;
            jumpKey = _jumpKey;
        }

        return grounded;
    }

    private void MyInput()
    {
        // Get Movement Keys
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && grounded)
            Jump();
    }

    private void Jump()
    {
        float finalJumpForce = jumpForce;

        // Player's jump force should be affected by upward momentum.
        if (addUpwardVerticalMomentumOnJump && rb.velocity.y > 0)
            finalJumpForce += rb.velocity.y;

        // Player's jump force should be affected by downward momentum.
        if (addDownwardVerticalMomentumOnJump && rb.velocity.y < 0)
            finalJumpForce += rb.velocity.y;

        rb.velocity = new Vector3(rb.velocity.x, finalJumpForce, rb.velocity.z);

        jumpKey = KeyCode.None;
    }

    // Get the direction the player wants to go (and normal to the slope if there is one).
    private Vector3 GetWishDir()
    {
        return _wishDir = Vector3.Cross(_slopeHit.normal, orientation.forward * _horizontalInput - orientation.right * _verticalInput).normalized;
    }

    // Get the current velocity in the WishDir direction.
    private float GetCurrentWishDirVel()
    {
        return currentWishDirVel = Vector3.Dot(_wishDir, rb.velocity);
    }

    private void HandleGroundAccel()
    {
        // The player is moving faster than their groundMaxVelocity in the _wishDir direction. Do not apply _wishDirGroundAccel.
        if (currentWishDirVel > groundMaxVelocity)
            _wishDirGroundAccel = _wishDir * 0f;

        // The player's velocity in the _wishDir direction will be greater than groundMaxVelocity in the next physics update.
        // Only apply enough acceleration in the _wishDir direction to reach groundMaxVelocity.
        else if (currentWishDirVel + groundAcceleration > groundMaxVelocity)
            _wishDirGroundAccel = _wishDir * (groundMaxVelocity - currentWishDirVel);

        // Just apply normal groundAcceleration in the _wishDir direction.
        else
            _wishDirGroundAccel = _wishDir * groundAcceleration;
    }

    private void HandleAirAccel()
    {
        // The player is moving faster than their airMaxVelocity in the _wishDir direction. Do not apply _wishDirAirAccel.
        if (currentWishDirVel > airMaxVelocity)
            _wishDirAirAccel = _wishDir * 0f;

        // The player's velocity in the _wishDir direction will be greater than airMaxVelocity in the next physics update.
        // Only apply enough acceleration in the _wishDir direction to reach airMaxVelocity.
        else if (currentWishDirVel + airAcceleration > airMaxVelocity)
            _wishDirAirAccel = _wishDir * (airMaxVelocity - currentWishDirVel);

        // Just apply normal airAcceleration in the _wishDir direction.
        else
            _wishDirAirAccel = _wishDir * airAcceleration;
    }

    private void HandleGroundFriction()
    {
        // The player's velocity is less than groundMinVelocity(the minimum velocity on the ground allowed before velocity gets set to 0).
        // Just zero-out the velocity.
        if (grounded && rb.velocity.magnitude < groundMinVelocity)
            rb.velocity -= rb.velocity;

        // Just apply normal groundFriction.
        else if (grounded)
            rb.velocity -= rb.velocity * groundFriction;
    }

    private void HandleAirFriction()
    {
        // The player's horizontal velocity is less than airMinVelocity(the minimum velocity in the air allowed before horizontal velocity gets set to 0).
        // Just zero-out the horizontal velocity.
        if (!grounded && new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).magnitude < airMinVelocity)
            rb.velocity -= new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);

        // Just apply normal airFriction on the horizontal axes.
        else if (!grounded)
            rb.velocity -= new Vector3(rb.velocity.x * airFriction, 0.0f, rb.velocity.z * airFriction);
    }

    private void HandleGravity()
    {
        if (!grounded)
            rb.velocity -= new Vector3(0f, gravity, 0f);
    }

    private void MovePlayer()
    {
        if (grounded)
            rb.velocity += _wishDirGroundAccel;
        else
            rb.velocity += _wishDirAirAccel;
    }
}