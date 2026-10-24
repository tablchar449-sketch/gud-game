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

    public float speed, groundSpeed, airSpeed, slideSpeed;
    public float dragLerpAcceleration;
    public float speedLerpAcceleration;
    public float jumpHeight;
    public Camera playerCam;
    public float groundDrag, airDrag;

    public GameObject cameraPosition;
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
    float originalScaleY;

    [Header("Perfect Slide")] 
    public GameObject slideCheckObject;
    public float slideRayCheckDistance;
    public float rayDistance;
    public float perfectSlideDistance;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float maxRayDistance;
    public bool isGrounded;
    public float maxSlopeAngle;
    
    [Header("Debug")]
    public GameObject velocityDebugObject;
    RaycastHit raycastHit;

    Vector3 moveDir;

    Vector2 moveInputDirection;


    public void moveInputs(InputAction.CallbackContext context)
    {
        moveInputDirection = context.ReadValue<Vector2>();   
    }

    public void jumpKey(InputAction.CallbackContext context)
    {
        if(isGrounded && context.performed)
        {
            isGrounded = false;
            rb.linearDamping = airDrag;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);

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
            if (!isGrounded)
            {
                Physics.SphereCast(slideCheckObject.transform.position, 0.5f, Vector3.down, out RaycastHit hit, slideRayCheckDistance + Mathf.Abs(rb.linearVelocity.y), groundLayer, QueryTriggerInteraction.Ignore);
                rayDistance = hit.distance;
            }
            rb.MovePosition(orientation.transform.position + new Vector3(0f,-0.5f,0f));
        }
        if (context.canceled)
        {
            wantToSlide = false;
            rb.MovePosition(orientation.transform.position + new Vector3(0f,0.5f,0f));
            transform.localScale = new Vector3(transform.localScale.x, originalScaleY, transform.localScale.z);
        }
    }







    void GroundDetection()
    {
        Physics.SphereCast(orientation.transform.position, 0.5f, Vector3.down, out raycastHit,maxRayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        //Physics.Raycast(orientation.transform.position, Vector3.down, out raycastHit, maxRayDistance, groundLayer, QueryTriggerInteraction.Ignore);
        if(raycastHit.collider != null)
        {
            isGrounded = true;
            rb.useGravity = false;
        }
        else{ isGrounded = false; rb.useGravity = true; }
    
    }


    void HandleDrag()//This is kinda sloppy. May want to change later but works for now
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

    void Move()
    {
        moveDir = orientation.transform.forward * moveInputDirection.y + orientation.transform.right * moveInputDirection.x;
        
        //Check if floor is above slope angle or not
        float floorAngle = Vector3.Dot(raycastHit.normal, Vector3.up);
        if(floorAngle >= maxSlopeAngle)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, raycastHit.normal).normalized;
        }
    }

    void Dash()
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
    

    void SlideCooldown()
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


    void Slide()
    {
        if(!wantToSlide)
        {
            maxRayDistance = originalScaleY; return;}
        
        if(isBoostReady && isGrounded && rb.linearVelocity.magnitude >= 8f)
        {
            isBoostReady = false;
            rb.AddForce(rb.linearVelocity.normalized * slideBoostForce, ForceMode.Impulse);
        }


        slideTimer = 0;
        maxRayDistance = slideHeight;
        transform.localScale = new Vector3(transform.localScale.x, slideHeight, transform.localScale.z);

        if (rayDistance <= perfectSlideDistance && rayDistance != 0)
        {
            //print(rayDistance);
            var tempVel = rb.linearVelocity;
            if (isGrounded)
            {
                rb.linearVelocity = Vector3.zero;
                Vector3 projVel = Vector3.ProjectOnPlane(tempVel, raycastHit.normal).normalized;
                projVel *= tempVel.magnitude;
                rb.AddForce(projVel, ForceMode.Impulse);
                rayDistance = 1;
                //print("Perfect slide");
            }
            
        }
        //Implement a perfect slide
        //How it works
        //Slide at the perfect time before hitting the floor, All your momentum gets converted into the slide boost
        //Use a raycast to determine how close the player is to the floor before sliding
    } 

    void SlideTurning()
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
        
        velocityDebugObject.transform.position = orientation.transform.position + rb.linearVelocity.normalized * 3;
        
        
        speedText.text = Convert.ToString(Mathf.Round(rb.linearVelocity.magnitude * 100) / 100);
    }


    private void FixedUpdate()
    {

        SlideTurning();
        rb.AddForce(moveDir * speed, ForceMode.VelocityChange);
    }
}
