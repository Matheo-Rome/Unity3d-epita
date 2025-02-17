using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = System.Random;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float swingSpeed;
    [SerializeField] private float groundDrag;
    
    [Header("Ground Check")]
    [SerializeField] private float playerHeigt;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Slope Handling")] 
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private float minSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    
    
    public bool grounded;
    
    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump = true;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float startYScale;

    [Header("Orientation")] 
    [SerializeField] private Transform orientation;

    [Header("Camera")] 
    [SerializeField] private PlayerCam pc;

    [Header("Grappling")]
    [SerializeField] private Swing sw;
    
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    [Header("Audio")]
    [SerializeField] private AudioSource jumpSound;
    [SerializeField] private AudioSource landSound;
    [SerializeField] private AudioSource walkingSound;

    public bool freeze;
    public bool activeGrapple;
    public bool swinging;

    GlobalAchievement ach;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        ach = FindObjectOfType<GlobalAchievement>();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void Update()
    {
        var oldG = grounded;
        // test if the player is on the ground
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeigt * 0.5f + 0.2f, whatIsGround);
        // if player was of ground and now is grounded play land sound
        if (!oldG && grounded)
            landSound.Play();
        
        
        MyInput();
        SpeedControl();
        StateHandler();
        
        //set drag and refuel for the swinging
        if (grounded && !activeGrapple)
        {
            rb.drag = groundDrag;
            if (sw.fuel < sw.maxfuel && sw.joint == null)
            {
                float percentage = 15 * Time.deltaTime;
                sw.fuel += sw.maxfuel * (percentage / 100f);
            }
        }
        else
        {
            if (sw.joint == null && sw.fuel < sw.maxfuel)
            {
                float percentage = 7 * Time.deltaTime;
                sw.fuel += sw.maxfuel * (percentage / 100f);
            }
            rb.drag = 0;
        }
        sw.IncrementFuel();
        
        
        if (rb.velocity.y < 0)
        {
           rb.velocity += Vector3.up * (Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime);
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        { 
            rb.velocity += Vector3.up * (Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime) ;
        }

        if (this.transform.position.y >= 100)
            ach.hundredMeter = true;

    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Jump
        if (Input.GetButtonDown("Jump") && readyToJump && grounded)
        {
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            Jump();
            jumpSound.Play();
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void StateHandler()
    {

        if (grounded && Input.GetButton("Sprint"))
        {
            walkingSound.pitch = 2f;
            moveSpeed = sprintSpeed;
        }
        else if (swinging)
        {
            moveSpeed = swingSpeed;
        }
        else if (grounded)
        {
            walkingSound.pitch = 1.5f;
            moveSpeed = walkSpeed;
        }
       
    }

    private void MovePlayer()
    {
        //when on Slope deactivate gravity (we "simulate it")
        rb.useGravity = !OnSlope();
        
        if (activeGrapple || swinging)
            return;
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //On slope : push the player in the direction of the slope (so he slides)
       if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // On ground : juste play the walking sound and push into the desired direction
        if (grounded)
        {
            if (moveDirection.normalized != Vector3.zero)
            {
                rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
                var wasPlaying = walkingSound.loop;
                walkingSound.loop = true;
                if (!wasPlaying)
                    walkingSound.Play();
            }
            else
            {
                if (readyToJump)
                    rb.constraints |=  RigidbodyConstraints.FreezePositionY;
                walkingSound.Stop();
                walkingSound.loop = false;
            }
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        // In air : stop walking sound and push the player a bit to increase air movement a bit
        else if (!grounded)
        {
            walkingSound.Stop();
            walkingSound.loop = false;
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        if (activeGrapple)
            return;
        //limite speed on slope
       if (OnSlope() && !exitingSlope && !swinging && grounded)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeigt * 0.5f + 0.3f) && !swinging)
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0 && angle > minSlopeAngle;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
    
}