using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

public class playerMovement : MonoBehaviour
{
    public float speed, groundSpeed, airSpeed;
    public float dragLerpAcceleration;
    public float speedLerpAcceleration;
    public float jumpHeight;
    public Camera playerCam;
    public float groundDrag, airDrag;
    private bool wantToJump;
    float timeSinceLastJump;
    public float jumpCooldown;
    public GameObject orientation;
    public Rigidbody rb;
    public TextMeshProUGUI speedText;

    [Header("Dashing")]
    public float dashForce;
    public float dashThresholdSpeed;
    //public float maxDashCharges;
    //public float currentDashCharge;
    //public float charge;
    //public float recharge;
    //public float rechargeRate;
    
    [Header("Sliding")]
    public float slideBoostCooldown;
    public float slideBoostForce;
    public float slideHeight;
    public bool wantToSlide;
    public bool isBoostReady;
    public float slideTimer;
    public float slideDrag;
    public float turnSpeed;
    public float turnAngle;
    public float minimumDrag;
    public float slideTime;
    float originalScaleY;

    [Header("Perfect Slide")] 
    public float perfectSlideTime;

    private bool startCheck;
    private float perfectSlideTimer;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float maxRayDistance;
    public bool isGrounded;
    public float maxSlopeAngle;
    
    [Header("Debug")]
    public GameObject velocityDebugObject;
    RaycastHit raycastHit;

    Vector3 moveDir;

    public Vector2 moveInputDirection;


    public void moveInputs(InputAction.CallbackContext context)
    {
        moveInputDirection = context.ReadValue<Vector2>();   
    }

    public void jumpKey(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            wantToJump = true;
        }
        else
        {
            wantToJump = false;
        }
    }

    public void dashKey(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Dash();
        }
    }

    public void slideKey(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            wantToSlide = true;
            rb.MovePosition(orientation.transform.position + new Vector3(0f,-0.5f,0f));
            startCheck = true;
        }
        if (context.canceled)
        {
            wantToSlide = false;
            rb.MovePosition(orientation.transform.position + new Vector3(0f,0.5f,0f));
            transform.localScale = new Vector3(transform.localScale.x, originalScaleY, transform.localScale.z);
        }
    }








    private void jump()
    {
        if (isGrounded && wantToJump && timeSinceLastJump >= jumpCooldown)
        {
            timeSinceLastJump = 0;
            isGrounded = false;
            rb.linearDamping = airDrag;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }
    
    private void GroundDetection()
    {
        Physics.SphereCast(orientation.transform.position, 0.5f, Vector3.down, out raycastHit,maxRayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        //Physics.Raycast(orientation.transform.position, Vector3.down, out raycastHit, maxRayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        if(raycastHit.collider != null)
        {
            isGrounded = true;
        }
        else{ isGrounded = false; }
    
    }


    private void HandleDrag()//This is kinda sloppy. May want to change later but works for now
    {
        if (isGrounded)
        {
            if (wantToSlide)
            {
                rb.linearDamping = slideDrag;
                speed = 0f;
            }
            else
            {
                if(moveInputDirection == Vector2.zero)
                {
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, groundDrag * 3, dragLerpAcceleration * 2 * Time.deltaTime); 
                }
                else
                {
                    if(rb.linearDamping > groundDrag){rb.linearDamping = groundDrag;}
                    rb.linearDamping = Mathf.Lerp(rb.linearDamping, groundDrag, dragLerpAcceleration * Time.deltaTime);
                }
                speed = Mathf.Lerp(speed, groundSpeed, speedLerpAcceleration * Time.deltaTime);
            
            }
        }
        else{
            rb.linearDamping = airDrag;
            speed = airSpeed;
        }
    }

    private void Move()
    {
        moveDir = orientation.transform.forward * moveInputDirection.y + orientation.transform.right * moveInputDirection.x;
        
        //Check if floor is above slope angle or not
        float floorAngle = Vector3.Dot(raycastHit.normal, Vector3.up);
        if(floorAngle >= maxSlopeAngle)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, raycastHit.normal).normalized;
        }
    }

    private void Dash()
    {
        //if (currentDashCharge == 0)
        //{ return; }

        Vector3 dashDirection = playerCam.transform.forward * moveInputDirection.y + playerCam.transform.right * moveInputDirection.x;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = playerCam.transform.forward;
        }

        if (rb.linearVelocity.magnitude <= dashThresholdSpeed)
        {
            rb.linearVelocity = Vector3.zero;
            rb.linearDamping = 0;
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        }
        else
        {
            Vector3 tempVel = rb.linearVelocity;
            rb.linearVelocity = Vector3.zero;
            rb.linearDamping = 0;
            rb.AddForce(dashDirection * tempVel.magnitude, ForceMode.Impulse);
        }
        //Change it so that if your current speed is above the max dash speed
        //The dash will just redirect your velocity instead (Add extra boost? to this or nah) 
    }

    /*void DashRefill()
    {
        currentDashCharge = Mathf.Floor(charge);
        charge += rechargeRate * Time.deltaTime;
        

    }*/
    

    private void SlideCooldown()
    {
        if(slideTimer >= slideBoostCooldown)
        {
            slideTimer = slideBoostCooldown;
            isBoostReady = true;
        }
        else
        {
            slideTimer += 1 * Time.deltaTime;
        }
    }


    private void Slide()
    {
        if(!wantToSlide)
        {
            maxRayDistance = originalScaleY;
            slideTime = 0;
            perfectSlideTimer = 0;
            startCheck = false;
            return;
        }

        if (isGrounded)
        {
            startCheck = false;
            slideTime += 1 * Time.deltaTime;
            
            var tempVelDir = rb.linearVelocity.normalized;
            var tempVelMagnitude = rb.linearVelocity.magnitude;
            if (perfectSlideTimer != 0)
            {
                var perfectTime = perfectSlideTimer;
                print(perfectTime);
                if (perfectTime <= perfectSlideTime)
                {
                    tempVelDir = Vector3.ProjectOnPlane(tempVelDir, raycastHit.normal).normalized;
                    rb.linearVelocity = Vector3.zero;
                    rb.AddForce(tempVelDir * tempVelMagnitude, ForceMode.Impulse);
                    perfectTime = 100;
                }
                perfectSlideTimer = 0;
            }
        }
        else
        {
            slideTime = 0;
            if(startCheck)
            {
                perfectSlideTimer += Time.deltaTime;
            }
        }
        
        
        if(isBoostReady && isGrounded && rb.linearVelocity.magnitude >= 8f)
        {
            isBoostReady = false;
            rb.AddForce(rb.linearVelocity.normalized * slideBoostForce, ForceMode.Impulse);
        }


        slideTimer = 0;
        maxRayDistance = slideHeight; //maxRayDistance is for ground check
        transform.localScale = new Vector3(transform.localScale.x, slideHeight, transform.localScale.z);

        
        //Change perfect slide to be determined based on the time rather than distance
        //Like in untitled tag game
        //Implement a perfect slide
        //How it works
        //Slide at the perfect time before hitting the floor, All your momentum gets converted into the slide boost
        //Use a raycast to determine how close the player is to the floor before sliding
    } 

    private void SlideTurning()
    {
        //Only do if sliding and on the ground and faster than regular walking speed
        if (!(wantToSlide && isGrounded && rb.linearVelocity.magnitude >= 7.5f))
        { slideDrag = 0f; return; }
        
        //Get current direction and velocity magnitude
        var currentVelocityDirection = rb.linearVelocity.normalized;
        var currentVelocityMagnitude = rb.linearVelocity.magnitude;
        
        //Get camera direction
        var cameraDirection = new Vector3(playerCam.transform.forward.x, 0, playerCam.transform.forward.z).normalized;
        
        //Find angle between 2 directions
        turnAngle = Vector3.SignedAngle(currentVelocityDirection, cameraDirection, Vector3.up);
        
        var drag = (0.01f * Mathf.Pow(1.061f,Mathf.Abs(turnAngle))) - 0.01f;
        drag = Mathf.Clamp(drag, 0, 15);
        //If Right key down
        if (moveInputDirection.x > 0 && turnAngle > 0)
        {
            if (drag <= minimumDrag)
            {
                slideDrag = 0f;
            }
            else
            {
                slideDrag = drag;
            }
            
            //print(currentVelocityMagnitude);
            Vector3 newVelocity = Vector3.RotateTowards(currentVelocityDirection, cameraDirection, turnSpeed * Time.fixedDeltaTime, 
                0.0f); //This is 0 as we handle magnitude separately
            rb.linearVelocity = newVelocity * currentVelocityMagnitude;
        }
        
        
        //If Left key
        else if (moveInputDirection.x < 0 && turnAngle < 0)
        {
            if (drag <= minimumDrag)
            {
                slideDrag = 0f;
            }
            else
            {
                slideDrag = drag;
            }
            
            Vector3 newVelocity = Vector3.RotateTowards(currentVelocityDirection, cameraDirection, turnSpeed * Time.fixedDeltaTime, 
                0.0f); //This is 0 as we handle magnitude separately
            rb.linearVelocity = newVelocity * currentVelocityMagnitude;
        }
        
    }
    

    
    
    
    
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        originalScaleY = transform.localScale.y;
        Cursor.lockState = CursorLockMode.Locked;
        maxSlopeAngle = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
    }

    // Update is called once per frame
    private void Update()
    {
        orientation.transform.rotation = Quaternion.Euler(orientation.transform.rotation.eulerAngles.x, playerCam.transform.rotation.eulerAngles.y, orientation.transform.rotation.eulerAngles.z);   
        GroundDetection();
        HandleDrag();
        Move();
        SlideCooldown();
        Slide();
        //DashRefill();

        timeSinceLastJump += Time.deltaTime;
        if (isGrounded && !wantToSlide)
        {
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
        }
        
        velocityDebugObject.transform.position = orientation.transform.position + rb.linearVelocity.normalized * 3;
        
        
        speedText.text = Convert.ToString(Mathf.Round(rb.linearVelocity.magnitude * 100) / 100);
        

    }


    private void FixedUpdate()
    {
        SlideTurning();
        jump();
        timeSinceLastJump = jumpCooldown;
        rb.AddForce(moveDir * speed, ForceMode.VelocityChange);
    }
}
