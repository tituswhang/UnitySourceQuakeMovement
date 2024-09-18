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

    [Header("Ground Check")]
    public float playerHeight = 2.0f;
    public float maxVerticalVelocityToBeAirborne = 8.0f;    // The maximum vertical velocity allowed before the player is considered airborne.
    public LayerMask whatIsGround;
    public bool grounded = false;
    public bool addUpwardVerticalMomentumOnJump = false;    // Should the jump velocity be affected by the player's current upward momentum? (Not implemented yet)
    public bool addDownwardVerticalMomentumOnJump = false;  // Should the jump velocity be affected by the player's current downward momentum? (Not implemented yet)


    [Header("Slope Check")]
    public float maxSlopeAngle = 40.0f;                     // The maximum angle allowed before the player is considered airborne.
    private RaycastHit slopeHit;                            // Structure used to get info back from the boxCast in IsGround().

    public Transform orientation;                           // Orientation of the camera/player model.

    private float horizontalInput;                          // left = -1, right = 1.
    private float verticalInput;                            // down = -1, up = 1.

    private Vector3 wishDir;                                // The direction the player wants to go (and normal to the slope if there is one).
    private Vector3 wishDirGroundAccel;                     // The ground acceleration in the wishDir direction.
    private Vector3 wishDirAirAccel;                        // The air acceleration in the wishDir direction.

    private float currentWishDirVel;                        // Current velocity in the WishDir direction.

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        groundMaxVelocity += groundMaxVelocity * groundFriction;
        airMaxVelocity += airMaxVelocity * airFriction;
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
        bool groundCheck = Physics.BoxCast(transform.position, transform.localScale * 0.5f, Vector3.down, out slopeHit, transform.rotation, playerHeight * 0.255f, whatIsGround);

        // Player is considered grounded if their velocity is below maxVerticalVelocityToBeAirborne(the maximum vertical velocity allowed before the player is considered airborne),
        // touching a ground(groundCheck), and the ground has an angle less than maxSlopeAngle.
        if (rb.velocity.y < maxVerticalVelocityToBeAirborne && groundCheck && Vector3.Angle(Vector3.up, slopeHit.normal) < maxSlopeAngle)
            grounded = true;
        else
            grounded = false;

        return grounded;
    }

    private void MyInput()
    {
        // Get Movement Keys
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

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
    }

    // Get the direction the player wants to go (and normal to the slope if there is one).
    private Vector3 GetWishDir()
    {
        return wishDir = Vector3.ProjectOnPlane((orientation.forward * verticalInput + orientation.right * horizontalInput), slopeHit.normal).normalized;
    }

    // Get the current velocity in the WishDir direction.
    private float GetCurrentWishDirVel()
    {
        return currentWishDirVel = Vector3.Dot(wishDir, rb.velocity);
    }

    private void HandleGroundAccel()
    {
        // The player is moving faster than their groundMaxVelocity in the wishDir direction. Do not apply wishDirGroundAccel.
        if (currentWishDirVel > groundMaxVelocity)
            wishDirGroundAccel = wishDir * 0f;

        // The player's velocity in the wishDir direction will be greater than groundMaxVelocity in the next physics update.
        // Only apply enough acceleration in the wishDir direction to reach groundMaxVelocity.
        else if (currentWishDirVel + groundAcceleration > groundMaxVelocity)
            wishDirGroundAccel = wishDir * (groundMaxVelocity - currentWishDirVel);

        // Just apply normal groundAcceleration in the wishDir direction.
        else
            wishDirGroundAccel = wishDir * groundAcceleration;
    }

    private void HandleAirAccel()
    {
        // The player is moving faster than their airMaxVelocity in the wishDir direction. Do not apply wishDirAirAccel.
        if (currentWishDirVel > airMaxVelocity)
            wishDirAirAccel = wishDir * 0f;

        // The player's velocity in the wishDir direction will be greater than airMaxVelocity in the next physics update.
        // Only apply enough acceleration in the wishDir direction to reach airMaxVelocity.
        else if (currentWishDirVel + airAcceleration > airMaxVelocity)
            wishDirAirAccel = wishDir * (airMaxVelocity - currentWishDirVel);

        // Just apply normal airAcceleration in the wishDir direction.
        else
            wishDirAirAccel = wishDir * airAcceleration;
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
            rb.velocity += wishDirGroundAccel;
        else
            rb.velocity += wishDirAirAccel;
    }
}