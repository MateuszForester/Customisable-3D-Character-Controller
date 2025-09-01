using UnityEngine;

public class DashModule : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCooldown = 1f;
    public float dashDistance = 50f;
    public float dashSpeed = 200f;
    public bool allowAirDash = true;
    public KeyCode dashKey = KeyCode.C;

    private ModularCharacterControllerScript controller;
    private CameraModule cameraModuleScript;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void Dash()
    {
        if (controller.dashCooldownTimer > 0f)
            controller.dashCooldownTimer -= Time.deltaTime;

        if (!controller.isDashing && controller.dashCooldownTimer <= 0f && Input.GetKeyDown(dashKey) && (controller.IsGrounded() || allowAirDash))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = Vector3.zero;
            if (controller.firstPersonActive)
                inputDir = controller.transform.TransformDirection(new Vector3(h, 0, v));
            else
            {
                Vector3 camForward = cameraModuleScript.playerCameraThirdPerson.forward;
                camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = cameraModuleScript.playerCameraThirdPerson.right;
                camRight.y = 0f; camRight.Normalize();
                inputDir = camForward * v + camRight * h;
            }

            if (inputDir.sqrMagnitude < 0.001f)
                inputDir = controller.transform.forward;

            controller.dashDirection = inputDir.normalized;
            controller.dashRemainingDistance = dashDistance;
            controller.isDashing = true;
            controller.dashCooldownTimer = dashCooldown;

            controller.animator.SetTrigger("IsDash");
        }

        if (controller.isDashing)
        {
            float dashStep = dashSpeed * Time.deltaTime;
            if (dashStep > controller.dashRemainingDistance)
                dashStep = controller.dashRemainingDistance;

            Vector3 move = controller.dashDirection * dashStep + Vector3.up * controller.velocity.y * Time.deltaTime;
            controller.playerCharacterController.Move(move);

            controller.dashRemainingDistance -= dashStep;
            if (controller.dashRemainingDistance <= 0f)
                controller.isDashing = false;
        }
    }
}
