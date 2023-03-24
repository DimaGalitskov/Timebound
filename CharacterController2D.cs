using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float jumpForce = 10.0f;
    [SerializeField] private float jumpVelocityFalloff;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool hasJumped;
    private PlayerInput inputActions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        inputActions = new PlayerInput();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => jumpInput = true;
        inputActions.Player.Jump.canceled += ctx => jumpInput = false;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleJump();
        HandleMovement();
        HandleGravity();
        Debug.Log(jumpInput);
    }

    [Header("Walking")]
    [SerializeField] private float walkSpeed = 4;
    [SerializeField] private float acceleration = 10;
    private void HandleMovement()
    {
        rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);
    }

    void HandleGravity()
    {
        if (rb.velocityY < jumpVelocityFalloff || rb.velocityY > 0 && jumpInput == false)
            rb.velocity += Vector2.up * Physics.gravity.y * fallMultiplier * Time.deltaTime;
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);
        if (isGrounded) hasJumped = false;
    }

    private void HandleJump()
    {
        if (jumpInput && !hasJumped)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            hasJumped = true;
        }
    }
}
