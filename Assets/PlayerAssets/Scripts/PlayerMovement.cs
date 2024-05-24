using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float groundAcceleration;
    public float groundMaxVelocity;
    public float groundFriction;
    
    public float airAcceleration;
    public float airMaxVelocity;
    public float airFriction;

    public float gravity;

    public float jumpForce;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode strafeForwardKey = KeyCode.W;
    public KeyCode strafeLeftKey = KeyCode.A;
    public KeyCode strafeBackwardKey = KeyCode.S;
    public KeyCode strafeRightKey = KeyCode.D;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;
    bool grounded;

    [Header("Slope Check")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 currentHorizontalVel;
    float currentWishDirVel;

    Vector3 wishDir;
    Vector3 wishDirSlope;
    Vector3 wishDirMaxGroundVel;
    Vector3 wishDirMaxAirVel;
    Vector3 wishDirGroundAccel;
    Vector3 wishDirAirAccel;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // MyInput();
    }

    private void FixedUpdate()
    {
        Debug.Log("horizontal vel: " + currentHorizontalVel.magnitude + "\nvertical vel: " + rb.velocity.y);
        // Debug.Log("grounded: " + grounded);

        MyInput();

        IsGrounded();

        HandleFriction();

        HandleGravity();

        GetHorizontalVelocity();

        WishDir();

        MovePlayer();
    }

    private void MyInput()
    {
        // get movement keys
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        if (Input.GetKey(jumpKey) && grounded)
            Jump();
    }

    private bool IsGrounded()
    {
        bool groundCheck = Physics.BoxCast(transform.position, transform.localScale * 0.5f, Vector3.down, out slopeHit, transform.rotation, playerHeight * 0.251f, whatIsGround);

        if (groundCheck && rb.velocity.y < jumpForce && Vector3.Angle(Vector3.up, slopeHit.normal) < maxSlopeAngle)
            grounded = true;
        else
            grounded = false;

        return grounded;
    }

    private void HandleFriction()
    {
        if (grounded)
            rb.drag = groundFriction;
        else
            rb.drag = airFriction;
    }

    private void HandleGravity()
    {
        if (grounded)
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z);
        else
            rb.velocity -= new Vector3(0f, gravity, 0f);
    }

    private Vector3 GetHorizontalVelocity()
    {
        currentHorizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (currentHorizontalVel.magnitude < 0.01f)
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        return currentHorizontalVel;
    }

    private void WishDir()
    {
        // movement direction
        wishDir = (orientation.forward * verticalInput + orientation.right * horizontalInput).normalized;

        wishDirMaxGroundVel = wishDir * groundMaxVelocity;

        wishDirMaxAirVel = wishDir * airMaxVelocity;

        currentWishDirVel = Vector3.Dot(wishDir, currentHorizontalVel);

        if (currentWishDirVel > groundMaxVelocity)
            wishDirGroundAccel = GetSlopeWishDir() * 0f;
        else if (currentWishDirVel + groundAcceleration > groundMaxVelocity)
            wishDirGroundAccel = GetSlopeWishDir() * (groundMaxVelocity - currentWishDirVel);
        else
            wishDirGroundAccel = GetSlopeWishDir() * groundAcceleration;

        if (currentWishDirVel > airMaxVelocity)
            wishDirAirAccel = wishDir * 0f;
        else if (currentWishDirVel + airAcceleration > airMaxVelocity)
            wishDirAirAccel = wishDir * (airMaxVelocity - currentWishDirVel);
        else
            wishDirAirAccel = wishDir * airAcceleration;
    }

    private void MovePlayer()
    {
        if (grounded)
            rb.velocity += wishDirGroundAccel;
        else
            rb.velocity += wishDirAirAccel;
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    private Vector3 GetSlopeWishDir()
    {
        wishDirSlope = Vector3.ProjectOnPlane(wishDir, slopeHit.normal).normalized;
        return wishDirSlope;
    }
}