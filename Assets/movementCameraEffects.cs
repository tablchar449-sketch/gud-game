using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class movementCameraEffects : MonoBehaviour
{
    public Rigidbody rb;
    public CinemachineCamera cinemachineCamera;
    public playerMovement ps;
    public float acceleration;
    public float originalfov;
    public float tiltAcceleration;
    public float tiltDegrees;
    float fov;
    bool doDashEffect;
    
    [Header("Particles")]
    public ParticleSystem dashParticles;
    
    
    public void Dash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            doDashEffect = true;
            if(ps.moveInputDirection == new Vector2(0,1) || ps.moveInputDirection == Vector2.zero)
            {
                dashParticles.Play();
            }
        }
    }


    void SlideTurning()
    {
        if (!ps.wantToSlide || !ps.isGrounded || !(ps.rb.linearVelocity.magnitude >= 7.5f) || ps.moveInputDirection.x == 0)
        {
            cinemachineCamera.Lens.Dutch = Mathf.Lerp(cinemachineCamera.Lens.Dutch, 0, tiltAcceleration * Time.deltaTime);
            return;
        }
        

        var turnDegrees = (ps.turnAngle / 180) * -tiltDegrees;
        //Right
        if (ps.moveInputDirection.x > 0 && ps.turnAngle > 0)
        {
            turnDegrees = Mathf.Clamp(turnDegrees, -45f, 0f); 
            cinemachineCamera.Lens.Dutch = Mathf.Lerp(cinemachineCamera.Lens.Dutch, turnDegrees, tiltAcceleration * Time.deltaTime);
        }
        //Left
        if (ps.moveInputDirection.x < 0 && ps.turnAngle < 0)
        {
            turnDegrees = Mathf.Clamp(turnDegrees, 0f, 45f); 
            cinemachineCamera.Lens.Dutch = Mathf.Lerp(cinemachineCamera.Lens.Dutch, turnDegrees, tiltAcceleration * Time.deltaTime);
        }

        else
        {
            cinemachineCamera.Lens.Dutch = Mathf.Lerp(cinemachineCamera.Lens.Dutch, 0, tiltAcceleration * Time.deltaTime);
        }



    }
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fov = originalfov;
    }

    void Update()
    {
        SlideTurning();
        cinemachineCamera.Lens.FieldOfView = fov;
        if (doDashEffect)
        {
            fov = Mathf.Lerp(fov,originalfov * 1.3f,acceleration * 5 * Time.deltaTime);
            if(fov >= (originalfov * 1.3f)- 0.1f )
            {
                doDashEffect = false;
                dashParticles.Stop();
            }
        }
        else
        {
            fov = Mathf.Lerp(fov, originalfov, acceleration * Time.deltaTime);
        }
    }
}
