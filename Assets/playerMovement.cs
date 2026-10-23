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
    public float dashForce;

    
    [Header("Sliding")]
    public float slideBoostCooldown;
    public float slideBoostForce;
    public float slideHeight;
    public bool wantToSlide;
    public bool isBoostReady;
    public float slideTimer;
    float originalScaleY;    


    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float maxRayDistance;
    public bool isGrounded;
    public float maxSlopeAngle;
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
        }
        if (context.canceled)
        {
            wantToSlide = false;
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
                rb.linearDamping = 0.2f;
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
        Vector3 dashDirection = playerCam.transform.forward * moveInputDirection.y + playerCam.transform.right * moveInputDirection.x;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = playerCam.transform.forward;
        }
        rb.linearVelocity = Vector3.zero;
        rb.linearDamping = 0;
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        //Change it so that if your current speed is above the max dash speed
        //The dash will just redirect your velocity instead (Add extra boost? to this or nah) 
    }

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
        if(wantToSlide == false)
        {
            transform.localScale = new Vector3(transform.localScale.x, originalScaleY, transform.localScale.z);
            maxRayDistance = originalScaleY;
            return;
        }

        if(isBoostReady == true && isGrounded && rb.linearVelocity.magnitude >= 8f)
        {
            isBoostReady = false;
            rb.AddForce(rb.linearVelocity.normalized * slideBoostForce, ForceMode.Impulse);
        }


        slideTimer = 0;
        maxRayDistance = slideHeight;
        transform.localScale = new Vector3(transform.localScale.x, slideHeight, transform.localScale.z);
    
        //Implement a perfect slide
        //How it works
        //Slide at the perfect time before hitting the floor, All your momentum gets converted into the slide boost
        //Use a raycast to determine how close the player is to the floor before sliding

    }

    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScaleY = transform.localScale.y;
        Cursor.lockState = CursorLockMode.Locked;
        maxSlopeAngle = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
    }

    // Update is called once per frame
    void Update()
    {
        orientation.transform.rotation = Quaternion.Euler(orientation.transform.rotation.eulerAngles.x, playerCam.transform.rotation.eulerAngles.y, orientation.transform.rotation.eulerAngles.z);   
        GroundDetection();
        HandleDrag();
        Move();
        SlideCooldown();
        Slide();


        speedText.text = Convert.ToString(rb.linearVelocity.magnitude);
    }


    void FixedUpdate()
    {

        rb.AddForce(moveDir * speed, ForceMode.VelocityChange);
    }
}
