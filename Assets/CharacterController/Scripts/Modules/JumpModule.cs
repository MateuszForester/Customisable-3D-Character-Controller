using UnityEngine;

public class JumpModule : MonoBehaviour
{
    [Header("Jumping Settings")]
    public float jumpHeight = 15f;
    public bool allowAirControl = true;
    public bool allowMultiJump = true;
    public bool omniDirectionalJump = true;
    public int maxAdditionalJumpCount = 2;

    [Header("Wall Bounce Settings")]
    public bool allowWallRunBounce = true;
    public bool allowFrontalWallBounce = true;
    public float wallBounceStrength = 20f;
    public float wallBounceUpwardBoost = 12f;
    public LayerMask wallMask;
    public float rotationSmoothSpeed = 10f;
    public float distanceFromWallToBounce = 3f;

    private ModularCharacterControllerScript controller;
    private CameraModule cameraModuleScript;

    public void Initialize(ModularCharacterControllerScript characterController, CameraModule cameraModule)
    {
        controller = characterController;
        cameraModuleScript = cameraModule;
        controller.wallBounceTargetRotation = controller.transform.rotation;
    }

    void Update()
    {
        if (controller.isWallBouncing)
        {
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                controller.wallBounceTargetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );

            if (controller.firstPersonActive && cameraModuleScript != null && cameraModuleScript.playerCameraFirstPerson != null)
            {
                cameraModuleScript.playerCameraFirstPerson.rotation = Quaternion.Slerp(
                    cameraModuleScript.playerCameraFirstPerson.rotation,
                    controller.wallBounceTargetRotation,
                    rotationSmoothSpeed * Time.deltaTime
                );
            }

            if (!controller.firstPersonActive)
            {
                Vector3 flatForward = controller.transform.forward;
                flatForward.y = 0f;
                flatForward.Normalize();
                controller.thirdPersonCameraYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            }
        }
    }

    public void Jump()
    {
        bool grounded = controller.IsGrounded();
        if (grounded)
        {
            controller.currentJumpCount = 0;
            controller.isWallBouncing = false;
        }

        if (!Input.GetButtonDown("Jump")) return;

        bool canJump = grounded || (allowMultiJump && controller.currentJumpCount < maxAdditionalJumpCount);
        if (!canJump && !allowWallRunBounce && !allowFrontalWallBounce) return;

        if (allowWallRunBounce && controller.isWallRunning)
        {
            DoBounce(controller.currentWallNormal, true);
            return;
        }

        if (allowFrontalWallBounce && !grounded)
        {
            RaycastHit hit;
            Vector3 rayOrigin = controller.transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, controller.transform.forward, out hit, distanceFromWallToBounce, wallMask))
            {
                DoBounce(hit.normal, false);
                return;
            }
        }

        if (canJump)
        {
            controller.totalVelocity.y = Mathf.Sqrt(jumpHeight * 2f * controller.gravity);
            if (!grounded) controller.currentJumpCount++;

            if (!grounded && omniDirectionalJump)
            {
                ApplyOmniDirectionalAirMovement();
            }
        }

    }

    private void DoBounce(Vector3 normal, bool fromWallRun)
    {
        Vector3 bounceDir = normal;
        if (!fromWallRun)
        {
            bounceDir.y = 0f;
        }
        bounceDir.Normalize();

        controller.totalVelocity = bounceDir * wallBounceStrength;
        controller.totalVelocity.y = wallBounceUpwardBoost;
        controller.horizontalVelocity = Vector3.zero;

        if (fromWallRun)
        {
            controller.isWallRunning = false;
            controller.wallRunCooldownActive = true;
            controller.wallRunCooldownTimer = controller.wallRunningModuleScript.wallRunCooldown;
        }
        else
        {
            controller.isWallClimbing = false;
        }

        controller.wallBounceTargetRotation = Quaternion.LookRotation(bounceDir, Vector3.up);
        controller.currentJumpCount = 0;
        controller.isWallBouncing = true;
    }

    private void ApplyOmniDirectionalAirMovement()
    {
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;

        if (h == 0f && v == 0f) return;

        Vector3 inputDir = Vector3.zero;

        if (controller.firstPersonActive)
            inputDir = controller.transform.TransformDirection(new Vector3(h, 0, v));
        else if (cameraModuleScript != null)
        {
            Vector3 camForward = cameraModuleScript.playerCameraThirdPerson.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraModuleScript.playerCameraThirdPerson.right;
            camRight.y = 0f;
            camRight.Normalize();

            inputDir = camForward * v + camRight * h;
        }

        if (inputDir.sqrMagnitude > 0.001f)
        {
            inputDir.Normalize();

            controller.totalVelocity.x = 0f;
            controller.totalVelocity.z = 0f;

            controller.horizontalVelocity = inputDir * controller.baseMovementSpeed;

            controller.isWallBouncing = false;
        }
    }
}