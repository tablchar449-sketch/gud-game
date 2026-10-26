using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class movementCameraEffects : MonoBehaviour
{
    public Rigidbody rb;
    public CinemachineCamera cinemachineCamera;
    public playerMovement ps;
    public float acceleration;
    public float originalfov;
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
    
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fov = originalfov;
    }

    void Update()
    {
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
