using UnityEngine;
using Unity.Cinemachine;

public class Player : MonoBehaviour
{
    [Header("Character Movement")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float dashSpeed = 5f;
    [SerializeField] private float rotationSpeed = 500f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float groundedTimer;
    public Vector3 velocity;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cam != null) cam.Priority = 10;    
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
    }

    // Movement handling function (moving, jumping, applying velocity)
    private void HandleMovement(){

        // grounded check
        if (controller.isGrounded)
        {
            groundedTimer = coyoteTime;
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        // handle move
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (movement != Vector3.zero)
        {
            Vector3 moveDir;

            if (cam != null)
            {

                Vector3 camForward = cam.transform.forward;
                Vector3 camRight = cam.transform.right;

                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDir = (camForward * movement.z + camRight * movement.x).normalized;

                controller.Move(moveDir * moveSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // handle jump
        if (Input.GetKeyDown(KeyCode.Space) && groundedTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            groundedTimer = 0f;
        }

        // handle gravity
        velocity.y += gravity * Time.deltaTime;


        // apply velocity
        controller.Move(velocity * Time.deltaTime);
    }
}
