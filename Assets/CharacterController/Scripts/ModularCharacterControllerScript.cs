using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModularCharacterControllerScript : MonoBehaviour
{
    internal CharacterController playerCharacterController;

    [Header("Additional Modules")]
    public bool cameraModule;
    public bool jumpModule;
    public bool sprintModule;
    public bool crouchModule;
    public bool dashModule;
    public bool wallRunModule;
    public bool animationModule;
    public bool wallClimbModule;

    [HideInInspector] public CameraModule cameraModuleScript;
    [HideInInspector] public JumpModule jumpModuleScript;
    [HideInInspector] public SprintModule sprintModuleScript;
    [HideInInspector] public CrouchModule crouchModuleScript;
    [HideInInspector] public DashModule dashModuleScript;
    [HideInInspector] public WallRunModule wallRunModuleScript;
    [HideInInspector] public AnimationModule animationModuleScript;
    [HideInInspector] public WallClimbModule wallClimbModuleScript;

    [Header("Base Settings")]
    public float baseMovementSpeed = 50f;
    public float gravity = 100f;
    public float capsuleRadius = 2.5f;
    public float capsuleHeight = 10f;
    public LayerMask groundAndWallMask;

    [Header("Keybinds")]
    public KeyCode lockMouseKey = KeyCode.Tab;
    public KeyCode moveForwardKey = KeyCode.W;
    public KeyCode moveBackwardKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    [ShowIfBool(nameof(cameraModule))]
    public KeyCode switchCameraKey = KeyCode.V;
    [ShowIfBool(nameof(jumpModule))]
    public KeyCode jumpKey = KeyCode.Space;
    [ShowIfBool(nameof(sprintModule))]
    public KeyCode sprintKey = KeyCode.LeftShift;
    [ShowIfBool(nameof(crouchModule))]
    public KeyCode crouchKey = KeyCode.C;
    [ShowIfBool(nameof(dashModule))]
    public KeyCode dashKey = KeyCode.F;
    [ShowIfBool(nameof(wallClimbModule))]
    public KeyCode climbKey = KeyCode.E;

    internal float firstPersonCameraPitch = 0f;
    internal float thirdPersonCameraTargetDistance;
    internal float thirdPersonCameraCurrentDistance;
    internal float thirdPersonCameraYaw = 0f;
    internal float thirdPersonCameraPitch = 20f;
    internal bool firstPersonActive = true;
    internal bool cursorLocked = true;

    internal Vector3 totalVelocity;
    internal Vector3 horizontalVelocity = Vector3.zero;
    internal bool isSprinting = false;
    internal bool isCrouching = false;

    internal float dashRemainingDistance;
    internal float dashCooldownTimer = 0f;
    internal bool isDashing = false;
    internal Vector3 dashDirection;

    internal int currentJumpCount = 0;
    internal bool isWallBouncing = false;
    internal Quaternion wallBounceTargetRotation;

    internal float wallRunCooldownTimer = 0f;
    internal float wallRunDurationTimer;
    internal float initialWallRunHeight;
    internal float currentWallRunSpeed;
    internal bool isWallOnRight;
    internal bool isWallOnLeft;
    internal bool isWallRunning = false;
    internal bool wallRunCooldownActive = false;
    internal Vector3 currentWallNormal;
    internal Vector3 wallRunDirection;
    internal Quaternion wallRunTargetPlayerRotation;

    internal bool isWallClimbing = false;
    internal bool isClimbKeyPressed = false;
    internal bool climbKeyConsumed = false;
    internal bool isMantling = false;
    internal Vector3 wallClimbMantleStartPos;
    internal Vector3 wallClimbMantleTargetPos;
    internal float wallClimbMantleTimer = 0f;

    internal Animator animator;

    private Vector3 capsuleTop;
    private Vector3 capsuleBottom;

    void OnValidate()
    {
        SetupModule(ref cameraModuleScript, cameraModule);
        SetupModule(ref jumpModuleScript, jumpModule);
        SetupModule(ref sprintModuleScript, sprintModule);
        SetupModule(ref crouchModuleScript, crouchModule);
        SetupModule(ref dashModuleScript, dashModule);
        SetupModule(ref wallRunModuleScript, wallRunModule);
        SetupModule(ref animationModuleScript, animationModule);
        SetupModule(ref wallClimbModuleScript, wallClimbModule);
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraModuleScript != null)
        {
            cameraModuleScript.ActivateFirstPersonCamera();
            thirdPersonCameraTargetDistance = cameraModuleScript.thirdPersonMaxDistance;
            thirdPersonCameraCurrentDistance = cameraModuleScript.thirdPersonMaxDistance;
        }

        if (cameraModule) cameraModuleScript?.Initialize(this);
        if (jumpModule) jumpModuleScript?.Initialize(this, cameraModuleScript);
        if (crouchModule) crouchModuleScript?.Initialize(this);
        if (sprintModule) sprintModuleScript?.Initialize(this, crouchModuleScript);
        if (dashModule) dashModuleScript?.Initialize(this, cameraModuleScript);
        if (wallRunModule) wallRunModuleScript?.Initialize(this);
        if (animationModule) animationModuleScript?.Initialize(this);
        if (wallClimbModule) wallClimbModuleScript?.Initialize(this);
    }

    void Update()
    {
        capsuleTop = transform.position;
        capsuleBottom = new Vector3(transform.position.x, capsuleTop.y - capsuleHeight, transform.position.z);
        MouseScreenLockToggle();

        if (!isMantling && playerCharacterController != null && playerCharacterController.enabled)
        {
            Movement();
            Gravity();
        }

        if (cameraModule) cameraModuleScript?.CameraSwitch();
        if (cameraModule) cameraModuleScript?.MouseLook();

        if (!isMantling)
        {
            if (sprintModule) sprintModuleScript?.Sprint();
            if (crouchModule) crouchModuleScript?.Crouch();
            if (dashModule) dashModuleScript?.Dash();
            if (jumpModule) jumpModuleScript?.Jump();
            if (wallRunModule) wallRunModuleScript?.HorizontalWallMovement();
        }

        if (wallClimbModule) wallClimbModuleScript?.WallClimbUpdate();
        if (animationModule) animationModuleScript?.Animations();

        DebugDrawCapsule(capsuleTop, capsuleBottom, capsuleRadius, IsGrounded() ? Color.green : Color.red);

        if (wallRunModuleScript != null)
        {
            Debug.DrawRay(transform.position, transform.right * wallRunModuleScript.wallCheckDistance, Color.red);
            Debug.DrawRay(transform.position, -transform.right * wallRunModuleScript.wallCheckDistance, Color.blue);
        }
    }

    void Gravity()
    {
        if (isWallClimbing && playerCharacterController.enabled)
            totalVelocity.y = 0f;
        else
            totalVelocity.y += -gravity * Time.deltaTime;

        playerCharacterController.Move(totalVelocity * Time.deltaTime);

        if (playerCharacterController.isGrounded && totalVelocity.y < 0)
        {
            totalVelocity.y = -2f;
            totalVelocity.x = 0f;
            totalVelocity.z = 0f;
        }
    }

    public bool IsGrounded()
    {
        return Physics.CheckCapsule(capsuleTop, capsuleBottom, capsuleRadius, groundAndWallMask);
    }

    Vector2 GetMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(moveLeftKey)) x -= 1f;
        if (Input.GetKey(moveRightKey)) x += 1f;

        if (Input.GetKey(moveBackwardKey)) y -= 1f;
        if (Input.GetKey(moveForwardKey)) y += 1f;

        Vector2 v = new Vector2(x, y);

        if (v.sqrMagnitude > 1f)
            v.Normalize();

        return v;
    }

    void Movement()
    {
        if (isWallRunning || isWallClimbing)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        Vector2 input = GetMoveInput();
        float horizontal = input.x;
        float vertical = input.y;

        Vector3 moveInput;

        if (firstPersonActive)
        {
            moveInput = transform.TransformDirection(new Vector3(horizontal, 0f, vertical));
        }
        else
        {
            if (cameraModuleScript == null)
                moveInput = Vector3.zero;
            else
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
        }
        else
        {
            if (jumpModuleScript != null && jumpModuleScript.allowAirControl)
                horizontalVelocity = moveInput * currentSpeed;
        }

        Vector3 totalMove = horizontalVelocity * Time.deltaTime + Vector3.up * totalVelocity.y * Time.deltaTime;
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
