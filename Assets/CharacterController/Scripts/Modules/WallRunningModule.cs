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

        controller.isWallOnRight = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallMask);
        controller.isWallOnLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallMask);

        if (controller.isWallOnRight)
            controller.currentWallNormal = rightHit.normal;
        else if (controller.isWallOnLeft)
            controller.currentWallNormal = leftHit.normal;
    }

    private bool CanStartWallRun()
    {
        if (controller.wallRunCooldownActive) return false;
        if (controller.IsGrounded()) return false;
        if (!(controller.isWallOnRight || controller.isWallOnLeft)) return false;

        float forwardInput = Input.GetAxisRaw("Vertical");
        if (forwardInput < 0.1f) return false;

        if (controller.totalVelocity.y <= 0f) return false;

        if (requireSprint && !controller.isSprinting) return false;

        return true;
    }

    private void StartWallRun()
    {
        controller.isWallRunning = true;
        controller.wallRunDurationTimer = 0f;

        controller.wallRunDirection = Vector3.ProjectOnPlane(transform.forward, controller.currentWallNormal).normalized;
        controller.wallRunTargetPlayerRotation = Quaternion.LookRotation(controller.wallRunDirection, Vector3.up);

        controller.initialWallRunHeight = transform.position.y;

        if (controller.totalVelocity.y < 0f)
            controller.totalVelocity.y = 0f;

        float forwardInput = Mathf.Clamp01(Input.GetAxisRaw("Vertical"));
        controller.currentWallRunSpeed = controller.isSprinting ? sprintWallRunSpeed : walkWallRunSpeed;
        controller.currentWallRunSpeed *= forwardInput;
    }

    private void StopWallRun()
    {
        controller.isWallRunning = false;
        controller.wallRunCooldownActive = true;
        controller.wallRunCooldownTimer = wallRunCooldown;
    }

    private void DoWallRun()
    {
        if (!(controller.isWallOnRight || controller.isWallOnLeft))
        {
            StopWallRun();
            return;
        }

        Vector3 wallForward = controller.wallRunDirection;
        transform.rotation = Quaternion.Slerp(transform.rotation, controller.wallRunTargetPlayerRotation, playerRotationSmoothSpeed * Time.deltaTime);

        float forwardInput = Input.GetAxisRaw("Vertical");
        if (forwardInput <= 0f)
        {
            StopWallRun();
            return;
        }

        Vector3 wallMove = wallForward * controller.currentWallRunSpeed * Time.deltaTime;

        float targetY = controller.initialWallRunHeight;
        float verticalDelta = targetY - transform.position.y;
        wallMove.y = verticalDelta;

        controller.playerCharacterController.Move(wallMove);
        controller.totalVelocity.y = Mathf.Max(controller.totalVelocity.y * Time.deltaTime, -controller.gravity);

        controller.wallRunDurationTimer += Time.deltaTime;
        if (controller.wallRunDurationTimer >= maxWallRunDuration)
        {
            StopWallRun();
            return;
        }
    }
}