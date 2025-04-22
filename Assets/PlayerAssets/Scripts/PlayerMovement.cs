using UnityEngine;
using UnityEngine.UIElements;

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
    public float maxDistanceFromGroundToBeGrounded = 0.1f;
    public LayerMask whatIsGround;
    public bool grounded = false;
    public bool addUpwardVerticalMomentumOnJump = false;
    public bool addDownwardVerticalMomentumOnJump = false;
    public float skin = 0.05f;

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
    private BoxCollider box;
    #region Unity Lifecycle
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        box = GetComponent<BoxCollider>();

        groundMaxVelocity += groundMaxVelocity * groundFriction;
        airMaxVelocity += airMaxVelocity * airFriction;

        _jumpKey = jumpKey;
    }

    private void FixedUpdate()
    {
        ResolveOverlaps();

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        // debug
        Debug.Log($"Velocity: {rb.velocity.magnitude}");
        //Debug.Log($"Velocity: {horizontalVelocity.magnitude}");

        MyInput();

        GetWishDir();
        GetCurrentWishDirVel();

        HandleGroundAccel();
        HandleGroundFriction();
        HandleAirAccel();
        HandleAirFriction();
        HandleGravity();

        MovePlayer();
    }
    #endregion
    private void ResolveOverlaps()
    {
        // Only check ground
        Collider[] overlaps = Physics.OverlapBox(
            box.bounds.center,
            box.bounds.extents + Vector3.one * skin,
            transform.rotation,
            whatIsGround,
            QueryTriggerInteraction.Ignore);

        foreach (var col in overlaps)
        {
            if (col == box) continue;
            if (Physics.ComputePenetration(
                    box, rb.position, transform.rotation,
                    col, col.transform.position, col.transform.rotation,
                    out Vector3 dir, out float dist))
            {
                rb.MovePosition(rb.position + dir * dist);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ResolveOverlaps();

        // Is this collider in the ground layer‑mask?
        if ((whatIsGround.value & (1 << collision.gameObject.layer)) == 0 || rb.velocity.y > maxVerticalVelocityToBeAirborne)
            return;

        // Get the bottom of the player's collider in the world space.
        float colliderBottom = GetComponent<Collider>().bounds.min.y;

        // Look for at least one contact whose normal is shallow enough to stand on.
        foreach (ContactPoint c in collision.contacts)
        {
            if (Vector3.Angle(Vector3.up, c.normal) <= maxSlopeAngle && colliderBottom - c.point.y <= maxDistanceFromGroundToBeGrounded)

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
        if (_wishDir.magnitude < 0.5f)
            HandleGroundFriction();

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
        if (currentWishDirVel > groundMaxVelocity || _wishDir.magnitude < 0.5f)
            _wishDirGroundAccel = Vector3.zero;
        else if (currentWishDirVel + groundAcceleration > groundMaxVelocity)
            _wishDirGroundAccel = _wishDir * (groundMaxVelocity - currentWishDirVel);
        else
            _wishDirGroundAccel = _wishDir * groundAcceleration;
    }

    private void HandleGroundFriction()
    {
        if (!grounded)
            return;

        if (rb.velocity.magnitude < groundMinVelocity && _wishDir.magnitude < 0.5f)
            rb.velocity = Vector3.zero;
        else
            rb.velocity -= rb.velocity * groundFriction;
    }

    private void HandleAirAccel()
    {
        if (currentWishDirVel > airMaxVelocity || _wishDir.magnitude < 0.5f)
            _wishDirAirAccel = Vector3.zero;
        else if (currentWishDirVel + airAcceleration > airMaxVelocity)
            _wishDirAirAccel = _wishDir * (airMaxVelocity - currentWishDirVel);
        else
            _wishDirAirAccel = _wishDir * airAcceleration;
    }

    private void HandleAirFriction()
    {
        if (grounded)
            return;

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        if (horizontalVelocity.magnitude < airMinVelocity && _wishDir.magnitude < 0.5f)
            rb.velocity -= horizontalVelocity;
        else
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