using UnityEngine;

public class ModularCharacterControllerScript : MonoBehaviour
{
    private CharacterController playerCharacterController;
    private Transform groundCheck;

    [Header("Basic Movement Settings")]
    public float baseMovementSpeed = 50f;
    public float gravity = 100f;
    public float capsuleGroundDistance = 0.2f;
    public float capsuleRadius = 4.5f;
    public float capsuleLength = 10f;
    public LayerMask groundMask;

    [Header("Jumping Settings")]
    public float jumpHeight = 15f;
    public bool allowAirControl = true; // toggle mid-air movement
    public bool allowMultiJump = true;       // toggle multiple jumps
    public int maxAdditionalJumpCount = 2;             // how many jumps before landing
    public bool omniDirectionalJump = true;  // if true, jumps can be in any direction
    private int currentJumpCount = 0;        // tracks jumps used

    [Header("Sprint Settings")]
    public float sprintSpeed = 75f; // extra speed while sprinting
    public bool sprintToggle = false; // true = toggle, false = hold
    public KeyCode sprintKey = KeyCode.LeftShift;
    
    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 15f; // movement speed while crouching
    public bool crouchToggle = false; // true = toggle, false = hold
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Dash Settings")]
    public float dashCooldown = 1f; // time in seconds between dashes
    public float dashDistance = 50f;
    public float dashSpeed = 200f;
    public bool allowAirDash = true;
    public KeyCode dashKey = KeyCode.C;

    [Header("Camera Settings")]
    public Transform playerCameraFirstPerson;
    public Transform playerCameraThirdPerson;
    public KeyCode switchCameraKey = KeyCode.V;
    public float mouseSensitivity = 2f;
    public KeyCode lockMouseKey = KeyCode.Tab;
    public float thirdPersonMinDistance = 15f;
    public float thirdPersonMaxDistance = 80f;
    public float thirdPersonScrollSensitivity = 10f;
    public LayerMask thirdPersonCameraCollisionMask;
    public float thirdPersonCollisionBuffer = 0.2f; // distance to keep from colliders

    [Header("Animation Settings")]
    public float transitionDuration = 0.05f; // adjust for smoothness

    private Vector3 velocity;
    private Vector3 capsuleTop;
    private Vector3 capsuleBottom;
    private Vector3 lastGroundedMove = Vector3.zero; // stores momentum before jump
    private Vector3 horizontalVelocity = Vector3.zero; // store horizontal velocity separately
    private Vector3 dashDirection;          // direction of the current dash
    private bool isCrouching = false;
    private bool firstPersonActive = true;
    private bool cursorLocked = true;
    private bool groundedCheck;
    private bool isSprinting = false;
    private bool isDashing = false;         // is a dash currently happening
    private float dashRemainingDistance;    // tracks remaining dash distance
    private float dashCooldownTimer = 0f; // tracks cooldown
    private float xRotation = 0f;
    private float thirdPersonTargetDistance;
    private float thirdPersonCurrentDistance;
    private float thirdPersonYaw = 0f;
    private float thirdPersonPitch = 20f; // start slightly above horizontal
    private Animator animator;

    void Start()
    {
        playerCharacterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        groundCheck = transform.Find("GroundCheck");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ActivateFirstPersonCamera();

        thirdPersonTargetDistance = thirdPersonMaxDistance;
        thirdPersonCurrentDistance = thirdPersonMaxDistance;
    }

    void Update()
    {
        capsuleTop = transform.position;
        capsuleBottom = new Vector3(transform.position.x, capsuleTop.y - capsuleLength, transform.position.z);
        MouseLockToggle();
        CameraSwitch();
        MouseLook();
        Sprint(); 
        Crouch();  
        Movement(); 
        Dash();
        Gravity();
        Jump();
        Animations(); // run animations last, after all states updated

        if (groundedCheck != (groundedCheck = IsGrounded()))
        {
            Debug.Log("Grounded state changed to: " + groundedCheck);
        }
        DebugDrawCapsule(capsuleTop, capsuleBottom, capsuleRadius, IsGrounded() ? Color.green : Color.red);
    }

    private string currentState = "";
    void Animations()
    {
        string nextState = "Idle"; // default

        if (isDashing)
        {
            nextState = "Dash";
        }
        else if (!IsGrounded())
        {
            nextState = "Jump";
        }
        else if (isCrouching)
        {
            nextState = (horizontalVelocity.magnitude > 0.1f) ? "CrouchWalk" : "CrouchIdle";
        }
        else if (horizontalVelocity.magnitude > 0.1f)
        {
            nextState = isSprinting ? "Run" : "Walk";
        }
        if (currentState != nextState)
        {
            animator.CrossFadeInFixedTime(nextState, transitionDuration);
            currentState = nextState;
        }
    }

    public bool IsGrounded()
    {
        bool isGrounded = Physics.CheckCapsule(capsuleTop, capsuleBottom, capsuleRadius, groundMask);
        return isGrounded;
    }

    void Movement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveInput = Vector3.zero;
        if (firstPersonActive)
            moveInput = transform.TransformDirection(new Vector3(horizontal, 0f, vertical));
        else
        {
            Vector3 camForward = playerCameraThirdPerson.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = playerCameraThirdPerson.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveInput = camForward * vertical + camRight * horizontal;
        }

        if (moveInput.sqrMagnitude > 0.001f)
            moveInput.Normalize();
        else
            moveInput = Vector3.zero; // explicitly zero if no input

        float currentSpeed = baseMovementSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;

        if (IsGrounded())
        {
            horizontalVelocity = moveInput * currentSpeed;
            lastGroundedMove = moveInput;
        }
        else
        {
            if (allowAirControl)
                horizontalVelocity = moveInput * currentSpeed;
            // else horizontalVelocity stays as-is
        }

        Vector3 totalMove = horizontalVelocity * Time.deltaTime + Vector3.up * velocity.y * Time.deltaTime;
        playerCharacterController.Move(totalMove);
    }

    void Gravity()
    {
        velocity.y += -gravity * Time.deltaTime;

        playerCharacterController.Move(velocity * Time.deltaTime);

        if (playerCharacterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }
    }

    void Jump()
    {
        bool grounded = IsGrounded();
        if (grounded) currentJumpCount = 0;

        if (Input.GetButtonDown("Jump"))
        {
            if (grounded || (allowMultiJump && currentJumpCount < maxAdditionalJumpCount))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);

                if (!grounded && omniDirectionalJump) // air jump
                {
                    // Get input at jump time
                    float h = 0f, v = 0f;
                    if (Input.GetKey(KeyCode.W)) v += 1f;
                    if (Input.GetKey(KeyCode.S)) v -= 1f;
                    if (Input.GetKey(KeyCode.A)) h -= 1f;
                    if (Input.GetKey(KeyCode.D)) h += 1f;

                    Vector3 inputDir = Vector3.zero;
                    if (firstPersonActive)
                        inputDir = transform.TransformDirection(new Vector3(h, 0, v));
                    else
                    {
                        Vector3 camForward = playerCameraThirdPerson.forward;
                        camForward.y = 0f; camForward.Normalize();
                        Vector3 camRight = playerCameraThirdPerson.right;
                        camRight.y = 0f; camRight.Normalize();
                        inputDir = camForward * v + camRight * h;
                    }

                    if (inputDir.sqrMagnitude > 0.001f)
                    {
                        inputDir.Normalize();
                        horizontalVelocity = inputDir * baseMovementSpeed; // **override horizontal velocity**
                    }
                }

                currentJumpCount++;
            }
        }
    }

    private void Sprint()
    {
        if (sprintToggle)
        {
            if (Input.GetKeyDown(sprintKey))
            {
                ToggleSprint();
            }
        }
        else
        {
            if (Input.GetKey(sprintKey))
            {
                SetSprint(true);
            }
            else
            {
                SetSprint(false);
            }
        }
    }

    void Crouch()
    {
        if (crouchToggle)
        {
            if (Input.GetKeyDown(crouchKey))
            {
                ToggleCrouch();
            }
        }
        else
        {
            if (Input.GetKey(crouchKey))
            {
                SetCrouch(true);
            }
            else
            {
                SetCrouch(false);
            }
        }
    }

    void Dash()
    {
        // Update cooldown timer
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Check for dash input
        if (!isDashing && dashCooldownTimer <= 0f && Input.GetKeyDown(dashKey) && (IsGrounded() || allowAirDash))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = Vector3.zero;
            if (firstPersonActive)
                inputDir = transform.TransformDirection(new Vector3(h, 0, v));
            else
            {
                Vector3 camForward = playerCameraThirdPerson.forward;
                camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = playerCameraThirdPerson.right;
                camRight.y = 0f; camRight.Normalize();
                inputDir = camForward * v + camRight * h;
            }

            if (inputDir.sqrMagnitude < 0.001f)
                inputDir = transform.forward;

            dashDirection = inputDir.normalized;
            dashRemainingDistance = dashDistance;
            isDashing = true;
            dashCooldownTimer = dashCooldown;

            // Trigger animation when dash *actually starts*
            animator.SetTrigger("IsDash");
        }

        // Execute dash
        if (isDashing)
        {
            float dashStep = dashSpeed * Time.deltaTime;
            if (dashStep > dashRemainingDistance)
                dashStep = dashRemainingDistance;

            Vector3 move = dashDirection * dashStep + Vector3.up * velocity.y * Time.deltaTime;
            playerCharacterController.Move(move);

            dashRemainingDistance -= dashStep;
            if (dashRemainingDistance <= 0f)
                isDashing = false;
        }
    }

    void ToggleSprint()
    {
        isSprinting = !isSprinting;
        SetCrouch(isSprinting);
    }

    void SetSprint(bool sprint)
    {
        isSprinting = sprint;
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        SetCrouch(isCrouching);
    }

    void SetCrouch(bool crouch)
    {
        isCrouching = crouch;
        playerCharacterController.height = crouch ? crouchHeight : standingHeight;
    }

    void CameraSwitch()
    {
        if (Input.GetKeyDown(switchCameraKey))
        {
            firstPersonActive = !firstPersonActive;
            if (firstPersonActive) ActivateFirstPersonCamera();
            else ActivateThirdPersonCamera();
        }

        MouseLook();
    }

    void ActivateFirstPersonCamera()
    {
        playerCameraFirstPerson.gameObject.SetActive(true);
        playerCameraThirdPerson.gameObject.SetActive(false);
    }

    void ActivateThirdPersonCamera()
    {
        playerCameraFirstPerson.gameObject.SetActive(false);
        playerCameraThirdPerson.gameObject.SetActive(true);
    }

    void MouseLook()
    {
        if (!cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (firstPersonActive)
        {
            // First-person: vertical rotation
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            playerCameraFirstPerson.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotate character with camera
            transform.Rotate(Vector3.up * mouseX);
        }
        else
        {
            // Third-person: orbit
            OrbitCamera(mouseX, mouseY);
        }
    }

    void OrbitCamera(float mouseX, float mouseY)
    {
        if (firstPersonActive) return;

        // Update yaw/pitch from mouse input
        thirdPersonYaw += mouseX;
        thirdPersonPitch -= mouseY;
        thirdPersonPitch = Mathf.Clamp(thirdPersonPitch, -80f, 80f);

        // Zoom input
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        thirdPersonTargetDistance -= scroll * thirdPersonScrollSensitivity;
        thirdPersonTargetDistance = Mathf.Clamp(thirdPersonTargetDistance, thirdPersonMinDistance, thirdPersonMaxDistance);

        // Rotate character to match camera horizontal yaw
        Vector3 camForward = Quaternion.Euler(0f, thirdPersonYaw, 0f) * Vector3.forward;
        float rotationSpeed = 10f; // adjust for smooth rotation
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(camForward),
                                              rotationSpeed * Time.deltaTime);

        // Compute pivot position
        Vector3 pivotPos = playerCameraThirdPerson.parent.position; // pivot at character's head or center

        // Calculate desired camera position from yaw/pitch
        Vector3 direction = Quaternion.Euler(thirdPersonPitch, thirdPersonYaw, 0f) * Vector3.forward;
        Vector3 desiredPos = pivotPos - direction * thirdPersonTargetDistance;

        // SphereCast collision to avoid clipping
        RaycastHit hit;
        float collisionRadius = 0.2f;
        if (Physics.SphereCast(pivotPos, collisionRadius, (desiredPos - pivotPos).normalized,
                               out hit, thirdPersonTargetDistance, thirdPersonCameraCollisionMask))
        {
            // Move camera slightly in front of collision
            thirdPersonCurrentDistance = Mathf.Clamp(hit.distance - thirdPersonCollisionBuffer,
                                                     thirdPersonMinDistance,
                                                     thirdPersonMaxDistance);
            desiredPos = pivotPos - direction * thirdPersonCurrentDistance;
        }
        else
        {
            thirdPersonCurrentDistance = thirdPersonTargetDistance;
        }

        // After calculating desiredPos with yaw/pitch and collision
        playerCameraThirdPerson.position = desiredPos; // directly set position
        playerCameraThirdPerson.LookAt(pivotPos);      // look at pivot

    }

    void MouseLockToggle()
    {
        if (Input.GetKeyDown(lockMouseKey))
        {
            cursorLocked = !cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cursorLocked;
        }
    }

    void DebugDrawCapsule(Vector3 start, Vector3 end, float radius, Color color)
    {
        // Draw top and bottom spheres
        Debug.DrawLine(start + Vector3.forward * radius, start - Vector3.forward * radius, color);
        Debug.DrawLine(start + Vector3.right * radius, start - Vector3.right * radius, color);
        Debug.DrawLine(end + Vector3.forward * radius, end - Vector3.forward * radius, color);
        Debug.DrawLine(end + Vector3.right * radius, end - Vector3.right * radius, color);

        // Draw lines connecting top and bottom
        Debug.DrawLine(start + Vector3.forward * radius, end + Vector3.forward * radius, color);
        Debug.DrawLine(start - Vector3.forward * radius, end - Vector3.forward * radius, color);
        Debug.DrawLine(start + Vector3.right * radius, end + Vector3.right * radius, color);
        Debug.DrawLine(start - Vector3.right * radius, end - Vector3.right * radius, color);
    }
}