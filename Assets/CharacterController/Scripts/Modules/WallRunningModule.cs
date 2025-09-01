using UnityEngine;

public class WallRunningModule : MonoBehaviour
{
    private ModularCharacterControllerScript controller;

    [Header("Wall Run Settings")]
    public float wallCheckDistance = 2f;
    public float wallRunCooldown = 1f;
    public LayerMask wallMask;
    public bool requireSprint = false;
    public float maxWallRunDuration = 3f;
    public float playerRotationSmoothSpeed = 5f;
    public float walkWallRunSpeed = 15f;
    public float sprintWallRunSpeed = 60f;

    public void Initialize(ModularCharacterControllerScript controller)
    {
        this.controller = controller;
    }

    public void HorizontalWallMovement()
    {
        CheckForWalls();

        if (controller.wallRunCooldownActive)
        {
            controller.wallRunCooldownTimer -= Time.deltaTime;
            if (controller.wallRunCooldownTimer <= 0f)
                controller.wallRunCooldownActive = false;
        }

        if (!controller.isWallRunning && CanStartWallRun())
        {
            StartWallRun();
        }

        if (controller.isWallRunning)
        {
            DoWallRun();
        }
    }

    private void CheckForWalls()
    {
        RaycastHit rightHit;
        RaycastHit leftHit;

        controller.wallOnRight = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallMask);
        controller.wallOnLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallMask);

        if (controller.wallOnRight)
            controller.wallNormal = rightHit.normal;
        else if (controller.wallOnLeft)
            controller.wallNormal = leftHit.normal;
    }

    private bool CanStartWallRun()
    {
        if (controller.wallRunCooldownActive) return false;
        if (controller.IsGrounded()) return false;
        if (!(controller.wallOnRight || controller.wallOnLeft)) return false;

        float forwardInput = Input.GetAxisRaw("Vertical");
        if (forwardInput < 0.1f) return false;

        if (requireSprint && !controller.isSprinting) return false;

        return true;
    }

    private void StartWallRun()
    {
        controller.isWallRunning = true;
        controller.currentWallRunTimer = 0f;

        controller.wallRunDirection = Vector3.ProjectOnPlane(transform.forward, controller.wallNormal).normalized;
        controller.targetPlayerRotation = Quaternion.LookRotation(controller.wallRunDirection, Vector3.up);

        controller.wallRunStartHeight = transform.position.y;

        if (controller.velocity.y < 0f)
            controller.velocity.y = 0f;

        float forwardInput = Mathf.Clamp01(Input.GetAxisRaw("Vertical"));
        controller.wallRunSpeed = controller.isSprinting ? sprintWallRunSpeed : walkWallRunSpeed;
        controller.wallRunSpeed *= forwardInput;
    }

    private void StopWallRun()
    {
        controller.isWallRunning = false;
        controller.wallRunCooldownActive = true;
        controller.wallRunCooldownTimer = wallRunCooldown;
    }

    private void DoWallRun()
    {
        if (!(controller.wallOnRight || controller.wallOnLeft))
        {
            StopWallRun();
            return;
        }

        Vector3 wallForward = controller.wallRunDirection;

        transform.rotation = Quaternion.Slerp(transform.rotation, controller.targetPlayerRotation, playerRotationSmoothSpeed * Time.deltaTime);

        float forwardInput = Input.GetAxisRaw("Vertical");
        if (forwardInput <= 0f)
        {
            StopWallRun();
            return;
        }
        Vector3 wallMove = wallForward * controller.wallRunSpeed * Time.deltaTime;

        float targetY = controller.wallRunStartHeight;
        float verticalDelta = targetY - transform.position.y;
        wallMove.y = verticalDelta;

        controller.playerCharacterController.Move(wallMove);

        controller.velocity.y = Mathf.Max(controller.velocity.y * Time.deltaTime, -controller.gravity);

        controller.currentWallRunTimer += Time.deltaTime;
        if (controller.currentWallRunTimer >= maxWallRunDuration)
        {
            StopWallRun();
            return;
        }
    }
}
