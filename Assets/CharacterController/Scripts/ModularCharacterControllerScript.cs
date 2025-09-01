using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModularCharacterControllerScript : MonoBehaviour
{
    internal CharacterController playerCharacterController;
    private Transform groundCheck;

    [Header("Additional Modules")]
    public bool cameraModule;
    public bool jumpModule;
    public bool sprintModule;
    public bool crouchModule;
    public bool dashModule;
    public bool wallRunningModule;
    public bool animationModule;

    [HideInInspector] public CameraModule cameraModuleScript;
    [HideInInspector] public JumpModule jumpModuleScript;
    [HideInInspector] public SprintModule sprintModuleScript;
    [HideInInspector] public CrouchModule crouchModuleScript;
    [HideInInspector] public DashModule dashModuleScript;
    [HideInInspector] public WallRunningModule wallRunningModuleScript;
    [HideInInspector] public AnimationModule animationModuleScript;

    [Header("Base Settings")]
    public float baseMovementSpeed = 50f;
    public float gravity = 100f;
    public float capsuleGroundDistance = 0.2f;
    public float capsuleRadius = 4.5f;
    public float capsuleLength = 10f;
    public KeyCode lockMouseKey = KeyCode.Tab;
    public LayerMask groundMask;

    internal float dashRemainingDistance;
    internal float dashCooldownTimer = 0f;
    internal float xRotation = 0f;
    internal float thirdPersonTargetDistance;
    internal float thirdPersonCurrentDistance;
    internal float thirdPersonYaw = 0f;
    internal float thirdPersonPitch = 20f;
    internal float wallRunCooldownTimer = 0f;
    internal float cameraRotationTimer = 0f;
    internal float currentWallRunTimer;
    internal float wallRunStartHeight;
    internal float finalWallMovementSpeed;
    internal float wallRunSpeed;
    internal bool wallOnRight;
    internal bool wallOnLeft;
    internal bool isSmoothingCameraRotation = false;
    internal bool isSprinting = false;
    internal bool isCrouching = false;
    internal bool isDashing = false;
    internal bool firstPersonActive = true;
    internal bool cursorLocked = true;
    internal bool isWallRunning = false;
    internal bool wallRunCooldownActive = false;
    internal Vector3 velocity;
    internal Vector3 horizontalVelocity = Vector3.zero;
    internal Vector3 dashDirection;
    internal Vector3 wallNormal;
    internal Vector3 wallRunMoveDirection = Vector3.zero;
    internal Vector3 wallRunVerticalVelocity = Vector3.zero;
    internal Vector3 cachedWallForward;
    internal Vector3 wallRunDirection;
    internal Animator animator;
    internal Quaternion targetPlayerRotation;
    internal Quaternion targetCameraRotation;

    private bool groundedCheck;
    private Vector3 capsuleTop;
    private Vector3 capsuleBottom;
    private Vector3 lastGroundedMove = Vector3.zero;

    void OnValidate()
    {
        SetupModule(ref cameraModuleScript, cameraModule);
        SetupModule(ref jumpModuleScript, jumpModule);
        SetupModule(ref sprintModuleScript, sprintModule);
        SetupModule(ref crouchModuleScript, crouchModule);
        SetupModule(ref dashModuleScript, dashModule);
        SetupModule(ref wallRunningModuleScript, wallRunningModule);
        SetupModule(ref animationModuleScript, animationModule);
    }

    void SetupModule<T>(ref T module, bool enabled) where T : Behaviour
    {
        if (enabled)
        {
            if (module == null)
                module = gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
            else
                module.enabled = true;
        }
        else
        {
            if (module != null)
            {
#if UNITY_EDITOR
                T moduleToRemove = module;
                EditorApplication.delayCall += () =>
                {
                    if (moduleToRemove != null)
                    {
                        DestroyImmediate(moduleToRemove);
                        EditorUtility.SetDirty(gameObject);
                    }
                };
                module = null;
#else
            Destroy(module);
            module = null;
#endif
            }
        }
    }

    void Start()
    {
        playerCharacterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        groundCheck = transform.Find("GroundCheck");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraModuleScript != null)
        {
            cameraModuleScript.ActivateFirstPersonCamera();
            thirdPersonTargetDistance = cameraModuleScript.thirdPersonMaxDistance;
            thirdPersonCurrentDistance = cameraModuleScript.thirdPersonMaxDistance;
        }


        if (cameraModule) cameraModuleScript?.Initialize(this);
        if (jumpModule) jumpModuleScript?.Initialize(this);
        if (sprintModule) sprintModuleScript?.Initialize(this);
        if (crouchModule) crouchModuleScript?.Initialize(this);
        if (dashModule) dashModuleScript?.Initialize(this);
        if (wallRunningModule) wallRunningModuleScript?.Initialize(this);
        if (animationModule) animationModuleScript?.Initialize(this);
    }

    void Update()
    {
        capsuleTop = transform.position;
        capsuleBottom = new Vector3(transform.position.x, capsuleTop.y - capsuleLength, transform.position.z);
        MouseScreenLockToggle();
        Movement();
        Gravity();
        if (cameraModule) cameraModuleScript?.CameraSwitch();
        if (cameraModule) cameraModuleScript?.MouseLook();
        if (sprintModule) sprintModuleScript?.Sprint();
        if (crouchModule) crouchModuleScript?.Crouch();
        if (dashModule) dashModuleScript?.Dash();
        if (jumpModule) jumpModuleScript?.Jump();
        if (wallRunningModule) wallRunningModuleScript?.HorizontalWallMovement();
        if (animationModule) animationModuleScript?.Animations();

        if (groundedCheck != (groundedCheck = IsGrounded()))
        {
            Debug.Log("Grounded state changed to: " + groundedCheck);
        }
        DebugDrawCapsule(capsuleTop, capsuleBottom, capsuleRadius, IsGrounded() ? Color.green : Color.red);

        if (wallRunningModuleScript != null)
        {
            Debug.DrawRay(transform.position, transform.right * wallRunningModuleScript.wallCheckDistance, Color.red);
            Debug.DrawRay(transform.position, -transform.right * wallRunningModuleScript.wallCheckDistance, Color.blue);
        }
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

        if (isWallRunning)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }


        if (firstPersonActive)
            moveInput = transform.TransformDirection(new Vector3(horizontal, 0f, vertical));
        else
        {
            if (cameraModuleScript != null)
            {
                Vector3 camForward = cameraModuleScript.playerCameraThirdPerson.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cameraModuleScript.playerCameraThirdPerson.right;
                camRight.y = 0f;
                camRight.Normalize();

                moveInput = camForward * vertical + camRight * horizontal;
            }
        }

        if (moveInput.sqrMagnitude > 0.001f)
            moveInput.Normalize();
        else
            moveInput = Vector3.zero; 

        float currentSpeed = baseMovementSpeed;
        if (isCrouching && crouchModuleScript != null) currentSpeed = crouchModuleScript.crouchSpeed;
        else if (isSprinting && sprintModuleScript != null) currentSpeed = sprintModuleScript.sprintSpeed;

        if (IsGrounded())
        {
            horizontalVelocity = moveInput * currentSpeed;
            lastGroundedMove = moveInput;
        }
        else
        {
            if (jumpModuleScript != null && jumpModuleScript.allowAirControl)
                horizontalVelocity = moveInput * currentSpeed;
        }

        Vector3 totalMove = horizontalVelocity * Time.deltaTime + Vector3.up * velocity.y * Time.deltaTime;
        playerCharacterController.Move(totalMove);
    }

    void MouseScreenLockToggle()
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
        Debug.DrawLine(start + Vector3.forward * radius, start - Vector3.forward * radius, color);
        Debug.DrawLine(start + Vector3.right * radius, start - Vector3.right * radius, color);
        Debug.DrawLine(end + Vector3.forward * radius, end - Vector3.forward * radius, color);
        Debug.DrawLine(end + Vector3.right * radius, end - Vector3.right * radius, color);

        Debug.DrawLine(start + Vector3.forward * radius, end + Vector3.forward * radius, color);
        Debug.DrawLine(start - Vector3.forward * radius, end - Vector3.forward * radius, color);
        Debug.DrawLine(start + Vector3.right * radius, end + Vector3.right * radius, color);
        Debug.DrawLine(start - Vector3.right * radius, end - Vector3.right * radius, color);
    }
}