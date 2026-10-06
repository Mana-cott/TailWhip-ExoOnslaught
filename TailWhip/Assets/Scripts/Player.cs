using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Character Movement")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float groundedTimer;
    public Vector3 velocity;

    [Header("Hovering")]
    [SerializeField] private Slider hoverSlider;
    [SerializeField] private float maxHoverTime = 2f;
    [SerializeField] private float hoverRechargeRate = 1f;
    [SerializeField] private float hoverSpeed = 10f;
    private float currentHoverFuel;
    private bool isHovering = false;

    [Header("Reticle")]
    [SerializeField] private CinemachineCamera cam;

    [SerializeField] private ReticleController reticle;
    [SerializeField] private LayerMask aimLayerMask = ~0;
    [SerializeField] private float defaultAimDistance = 50f;
    [SerializeField] private float maxAimYaw = 12f;
    private Vector3 currAimWorldPoint;

    [Header("Spin Attack")]
    [SerializeField] private float spinFuelDrainRate = 1f;
    [SerializeField] private float spinMoveSpeed = 5f;
    [SerializeField] private float minFuelToSpin = 0.2f;
    private bool isSpinning = false;
    public bool IsSpinning => isSpinning;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cam != null) cam.Priority = 10;
        
        currentHoverFuel = maxHoverTime;
        if (hoverSlider != null)
        {
            hoverSlider.maxValue = maxHoverTime;
            hoverSlider.value = currentHoverFuel;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        UpdateHoverSlider();
        UpdateAnimations();
    }

    // Movement handling function (moving, jumping, applying velocity)
    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        // grounded check
        if (isGrounded)
        {
            isHovering = false;
            groundedTimer = coyoteTime;

            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (currentHoverFuel < maxHoverTime && !isSpinning)
            {
                currentHoverFuel += hoverRechargeRate * Time.deltaTime;
                currentHoverFuel = Mathf.Min(currentHoverFuel, maxHoverTime);
            }
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        HandleSpin();

        // handle move
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        GetCameraBasis(out Vector3 camForward, out Vector3 camRight);
        
        Vector3 input = new Vector3(horizontal, 0f, vertical);
        Vector3 moveDir = input.sqrMagnitude > 0.01f ? (camForward * input.z + camRight * input.x).normalized : Vector3.zero;

        if (reticle != null) reticle.UpdateReticle(horizontal);

        float sway = reticle != null ? reticle.GetNormalizedOffset() : 0f;
        Vector3 aimDir = Quaternion.AngleAxis(sway * maxAimYaw, Vector3.up) * camForward;
        Quaternion targetRotation = Quaternion.LookRotation(aimDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        UpdateAimTargetToPoint();

        // handle jump
        if (Input.GetKeyDown(KeyCode.Space) && groundedTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            groundedTimer = 0f;
        }

        if (!isGrounded && !isSpinning && Input.GetKey(KeyCode.Space) && currentHoverFuel > 0f && velocity.y <= 0.5f)
        {
            isHovering = true;
            currentHoverFuel -= Time.deltaTime;

            if (currentHoverFuel <= 0f)
            {
                currentHoverFuel = 0f;
                isHovering = false;
            }
        }
        else
        {
            isHovering = false;
        }

        if (isHovering)
        {
            velocity.y = 0f;
        } 
        else if (!isGrounded)
        {
            // handle gravity
            velocity.y += gravity * Time.deltaTime;
        }

        // apply velocity
        float currentMoveSpeed = isHovering ? hoverSpeed : moveSpeed;
        Vector3 horizontalVelocity = moveDir * currentMoveSpeed;
        Vector3 finalVelocity = horizontalVelocity + velocity;
        
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void HandleSpin()
    {
        bool wantsSpin = Input.GetMouseButton(1);

        if (!isSpinning)
        {
            if(wantsSpin && currentHoverFuel >= minFuelToSpin)
            {
                isSpinning = true;
            }
        }
        else
        {
            currentHoverFuel -= spinFuelDrainRate * Time.deltaTime;

            if (!wantsSpin || currentHoverFuel <= 0f)
            {
                currentHoverFuel = Mathf.Max(currentHoverFuel, 0f);
                isSpinning = false;
            }
        }
    }
    private void UpdateHoverSlider()
    {
        if (hoverSlider != null)
        {
            hoverSlider.value = currentHoverFuel;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);

        bool grounded = controller.isGrounded;
        animator.SetBool("IsInAir", !grounded);

        bool jumping = !grounded && velocity.y > 0.1f;
        animator.SetBool("IsJumping", jumping);

        animator.SetBool("IsHovering", isHovering);

        animator.SetBool("IsSpinning", isSpinning);
    }

    private void UpdateAimTargetToPoint()
    {
        Camera camera = Camera.main;
        if (camera == null || reticle == null) return;

        Ray ray = camera.ScreenPointToRay(reticle.GetReticlePosition());

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, aimLayerMask))
        {
            currAimWorldPoint = hit.point;
        }
        else
        {
            currAimWorldPoint = ray.GetPoint(defaultAimDistance);
        }
    }

    private void GetCameraBasis(out Vector3 forward, out Vector3 right)
    {
        Transform camTransform = cam != null ? cam.transform : Camera.main.transform;
    
        forward = camTransform.forward;
        right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = camTransform.up;
            forward.y = 0f;
        }

        forward.Normalize();
        right.Normalize();
    }

    public Vector3 GetAimWorldPoint()
    {
        return currAimWorldPoint;
    }

    public void PlayShootAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }
}
