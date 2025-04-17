using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    #region Inspector
    [Header("Movement")]
    public float groundAcceleration = 0.85f;
    public float groundMaxVelocity = 7.5f;
    public float groundMinVelocity = 0.5f;
    public float groundFriction = 0.1f;

    public float airAcceleration = 1.0f;
    public float airMaxVelocity = 1.0f;
    public float airMinVelocity = 0.5f;
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
    public float maxVerticalVelocityToBeAirborne = 8.0f;
    public LayerMask whatIsGround;
    public bool grounded = false;
    public bool addUpwardVerticalMomentumOnJump = false;
    public bool addDownwardVerticalMomentumOnJump = false;

    [Header("Slope Check")]
    public float maxSlopeAngle = 40.0f;
    public Transform orientation;
    #endregion

    /* ------------  private  ------------ */
    private RaycastHit _slopeHit;
    private float _horizontalInput;
    private float _verticalInput;
    private Vector3 _wishDir;
    private Vector3 _wishDirGroundAccel;
    private Vector3 _wishDirAirAccel;
    private float currentWishDirVel;
    private KeyCode _jumpKey;
    private Rigidbody rb;
    #region Unity Lifecycle
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
        // debug
        Debug.Log($"Velocity: {rb.velocity.magnitude}");

        MyInput();

        GetWishDir();
        GetCurrentWishDirVel();

        HandleGroundAccel();
        HandleAirAccel();
        HandleGroundFriction();
        HandleAirFriction();
        HandleGravity();

        MovePlayer();
    }
    #endregion
    private void OnCollisionStay(Collision collision)
    {
        // Is this collider in the ground layer‑mask?
        if ((whatIsGround.value & (1 << collision.gameObject.layer)) == 0 || rb.velocity.y > maxVerticalVelocityToBeAirborne)
            return;

        // Look for at least one contact whose normal is shallow enough to stand on.
        foreach (ContactPoint c in collision.contacts)
        {
            if (Vector3.Angle(Vector3.up, c.normal) <= maxSlopeAngle)
            {
                grounded = true;
                _slopeHit.normal = c.normal;   // preserve slope info for wish‑dir math
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if ((whatIsGround.value & (1 << collision.gameObject.layer)) != 0)
        {
            grounded = false;
            jumpKey = _jumpKey;      // reset jump buffer
            _slopeHit.normal = Vector3.up;
        }
    }

    private void MyInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && grounded)
            Jump();
    }

    private void Jump()
    {
        float finalJumpForce = jumpForce;

        if (addUpwardVerticalMomentumOnJump && rb.velocity.y > 0) finalJumpForce += rb.velocity.y;
        if (addDownwardVerticalMomentumOnJump && rb.velocity.y < 0) finalJumpForce += rb.velocity.y;

        rb.velocity = new Vector3(rb.velocity.x, finalJumpForce, rb.velocity.z);

        jumpKey = KeyCode.None;   // consume buffered key
        grounded = false;
    }

    private Vector3 GetWishDir() =>
        _wishDir = Vector3.Cross(_slopeHit.normal, orientation.forward * _horizontalInput - orientation.right * _verticalInput)
                   .normalized;

    private float GetCurrentWishDirVel() =>
        currentWishDirVel = Vector3.Dot(_wishDir, rb.velocity);

    private void HandleGroundAccel()
    {
        if (currentWishDirVel > groundMaxVelocity)
            _wishDirGroundAccel = Vector3.zero;
        else if (currentWishDirVel + groundAcceleration > groundMaxVelocity)
            _wishDirGroundAccel = _wishDir * (groundMaxVelocity - currentWishDirVel);
        else
            _wishDirGroundAccel = _wishDir * groundAcceleration;
    }

    private void HandleAirAccel()
    {
        if (currentWishDirVel > airMaxVelocity)
            _wishDirAirAccel = Vector3.zero;
        else if (currentWishDirVel + airAcceleration > airMaxVelocity)
            _wishDirAirAccel = _wishDir * (airMaxVelocity - currentWishDirVel);
        else
            _wishDirAirAccel = _wishDir * airAcceleration;
    }

    private void HandleGroundFriction()
    {
        if (grounded && rb.velocity.magnitude < groundMinVelocity)
            rb.velocity = Vector3.zero;
        else if (grounded)
            rb.velocity -= rb.velocity * groundFriction;
    }

    private void HandleAirFriction()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        if (!grounded && horizontalVelocity.magnitude < airMinVelocity)
            rb.velocity -= horizontalVelocity;
        else if (!grounded)
            rb.velocity -= new Vector3(horizontalVelocity.x * airFriction, 0, horizontalVelocity.z * airFriction);
    }

    private void HandleGravity()
    {
        if (!grounded)
            rb.velocity -= new Vector3(0, gravity, 0);
    }

    private void MovePlayer()
    {
        rb.velocity += grounded ? _wishDirGroundAccel : _wishDirAirAccel;
    }
}
