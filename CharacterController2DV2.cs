using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController2DV2 : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float jumpForce = 10.0f;
    [SerializeField] private LayerMask groundLayer;

    private CharacterController cc;
    private bool isGrounded;
    private Vector2 moveInput;
    private float jumpInput;

    private PlayerInput inputActions;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        inputActions = new PlayerInput();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => jumpInput = 1;
        inputActions.Player.Jump.canceled += ctx => jumpInput = 0;
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
        HandleJump();
        HandleMovement();
    }

    private void HandleMovement()
    {
        cc.Move(new Vector3(moveInput.x * moveSpeed, cc.velocity.y, 0));
    }

    private void HandleJump()
    {
        if (jumpInput > 0 && cc.isGrounded)
        {
            jumpInput = 0;
        }
    }
}
