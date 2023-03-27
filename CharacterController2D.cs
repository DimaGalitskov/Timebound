using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isFacingRight;
    private bool isDashAttackTimeout;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool dashInput;
    private bool strikeInput;
    private bool hasJumped;
    private bool hasDashed;
    private bool isStriking;
    private PlayerInput inputActions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        isFacingRight = true;
        SetGravityScale(gravityScale);

        inputActions = new PlayerInput();
        inputActions.Player.Move.performed += ctx => { moveInput = ctx.ReadValue<Vector2>(); CheckDirectionToFace(moveInput.x > 0); };
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.started += ctx => { jumpInputTimer = jumpInputWindow; jumpInput = true; };
        inputActions.Player.Jump.canceled += ctx => jumpInput = false;
        inputActions.Player.Dash.started += ctx => dashInput = true;
        inputActions.Player.Dash.canceled += ctx => dashInput = false;
        inputActions.Player.Strike.performed += ctx => strikeInput = true;
        inputActions.Player.Strike.canceled += ctx => strikeInput = false;
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
        HandleWalking(1);
        HandleGravity();
        HandleDash();
        HandleStrike();
    }

    [Header("Walking")]
    [SerializeField] private float walkSpeed = 4;
    [SerializeField] private float acceleration = 10;
    [SerializeField] private float deceleration = 20;
    [SerializeField] private float airMultiplier = .5f;
    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * walkSpeed;
        float speedDifference = targetSpeed - rb.velocityX;
        float appliedMovement = speedDifference * acceleration;
        rb.AddForce(appliedMovement * Vector2.right, ForceMode2D.Force);
    }

    private void HandleWalking(float lerpAmount)
    {
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = moveInput.x * walkSpeed;
        //We can reduce our control using Lerp() this smooths changes to our direction and speed
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        float accelRate;
        if (groundedTimer > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration * airMultiplier : deceleration * airMultiplier;

        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && groundedTimer < 0)
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }

        //Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - rb.velocity.x;
        //Calculate force along x-axis to apply to thr player
        float movement = speedDif * accelRate;
        //Convert this to a vector and apply to rigidbody
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 5;
    [SerializeField] private float jumpVelocityFalloff = 3;
    [SerializeField] private float fallMultiplier = 10;
    [SerializeField] private float maxFallSpeed = 10;
    void HandleGravity()
    {
        if (isDashAttackTimeout) SetGravityScale(0);
        else if (rb.velocityY < jumpVelocityFalloff || rb.velocityY > 0 && jumpInput == false) {rb.velocity += Vector2.down * gravityScale * fallMultiplier * Time.deltaTime; rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed)); }
        else SetGravityScale(gravityScale);
    }

    private void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    [Header("Grounded")]
    [SerializeField] private float coyoteTime;
    private float groundedTimer;
    private void CheckGrounded()
    {
        groundedTimer -= Time.deltaTime;
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);
        if (isGrounded) groundedTimer = coyoteTime;
        if (isGrounded) hasJumped = false;
    }

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 20;
    [SerializeField] private float jumpInputWindow;
    private float jumpInputTimer;
    private void HandleJump()
    {
        jumpInputTimer -= Time.deltaTime;
        if (jumpInputTimer >0 && !hasJumped && groundedTimer>0)
        {
            hasJumped = true;
            float force = jumpForce;
            if (rb.velocityY < 0) force -= rb.velocityY;
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        }
    }

    [Header("Dashing")]
    [SerializeField] private float dashForce = 20;
    [SerializeField] private float dashFreezeTime;
    [SerializeField] private float dashAttackTimeout;
    [SerializeField] private float dashEndTimeout;
    Vector2 lastDashDirection;
    private void HandleDash()
    {
        lastDashDirection = isFacingRight ? Vector2.right : Vector2.left;
        if(dashInput && !hasDashed)
        {
            hasDashed = true;
            Sleep(dashFreezeTime);
            StartCoroutine(nameof(StartDash), lastDashDirection);
        }
    }
    private IEnumerator StartDash(Vector2 direction)
    {
        isDashAttackTimeout = true;
        float startTime = Time.time;
        //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
        while (Time.time - startTime <= dashAttackTimeout)
        {
            rb.velocity = direction.normalized * dashForce;
            //Pauses the loop until the next frame, creating something of a Update loop. 
            //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
            yield return null;
        }
        isDashAttackTimeout = false;
        startTime = Time.time;
        //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
        rb.velocity = walkSpeed * direction.normalized;
        while (Time.time - startTime <= dashEndTimeout)
        {
            yield return null;
        }
        //Dash over
        hasDashed = false;
    }

    [Header("Striking")]
    [SerializeField] private GameObject striker;
    [SerializeField] private GameObject strikeParticle;
    [SerializeField] private int strikerCount;
    [SerializeField] private float strikeRange;
    [SerializeField] private float strikeStartDelay;
    [SerializeField] private float strikeRate;
    [SerializeField] private float strikeEndDelay;
    [SerializeField] private float strikeFreezeTime;
    [SerializeField] private float strikeCooldownTime;
    [SerializeField] private LayerMask strikeLayer;
    private float strikeTimer;
    private void HandleStrike()
    {
        strikeTimer -= Time.deltaTime;
        lastDashDirection = isFacingRight ? Vector2.right : Vector2.left;
        if (strikeInput && !isStriking && !hasDashed && strikeTimer < 0)
        {
            isStriking = true;
            Sleep(strikeFreezeTime);
            StartCoroutine(nameof(StartStrike), lastDashDirection);
        }
    }
    private IEnumerator StartBarrage(Vector2 direction)
    {
        yield return new WaitForSeconds(strikeStartDelay);
        var count = strikerCount;
        while (strikeInput && count>0)
        {
            float strikeAngle = isFacingRight ? -90 : 90;
            Quaternion strikeRotation = Quaternion.Euler(0, 0, strikeAngle);
            var instance = Instantiate(strikeParticle, transform.position, strikeRotation);
            Destroy(instance, 0.5f);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, strikeRange, strikeLayer);
            if (hit) { var particle = Instantiate(strikeParticle, hit.point, strikeParticle.transform.rotation); Destroy(particle, 0.5f); }
            count--;
            yield return new WaitForSeconds(strikeRate);
        }
        yield return new WaitForSeconds(strikeEndDelay);
        strikeTimer = strikeCooldownTime;
        isStriking = false;
    }
    private IEnumerator StartStrike(Vector2 direction)
    {
        yield return new WaitForSeconds(strikeStartDelay);
        float strikeAngle = isFacingRight ? -90 : 90;
        Quaternion strikeRotation = Quaternion.Euler(0, 0, strikeAngle);
        var instance = Instantiate(strikeParticle, transform.position, strikeRotation);
        Destroy(instance, 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, strikeRange, strikeLayer);
        if (hit) { var particle = Instantiate(strikeParticle, hit.point, strikeParticle.transform.rotation); Destroy(particle, 0.5f); }
        yield return new WaitForSeconds(strikeEndDelay);
        isStriking = false;
    }



    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        //nameof() notation means we don't need to input a string directly.
        //Removes chance of spelling mistakes and will improve error messages if any
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(duration); //Must be Realtime since timeScale with be 0 
        Time.timeScale = 1;
    }

    private void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight) isFacingRight = !isFacingRight;
    }
}
