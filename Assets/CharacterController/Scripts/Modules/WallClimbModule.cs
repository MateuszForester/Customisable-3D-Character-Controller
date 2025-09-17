using System.Collections;
using UnityEngine;

public class WallClimbModule : MonoBehaviour
{
    [Header("Wall Climb Settings")]
    public KeyCode climbKey = KeyCode.E;
    public float wallCheckDistance = 2f;
    public LayerMask wallMask;
    public LayerMask groundMask;
    public float maxClimbAngle = 45f;
    public float wallStickForce = 5f;
    public bool climbToggle = false;
    public float horizontalClimbSpeed = 5f;
    public float verticalClimbSpeed = 5f;
    public float cornerTurnSpeed = 15f;

    [Header("Mantle Settings")]
    public float mantleDistance = 1f;
    public float mantleDuration = 0.4f;

    private ModularCharacterControllerScript controller;
    
    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void WallClimbUpdate()
    {
        if (controller == null || controller.playerCharacterController == null)
            return;

        if (controller.isMantling)
        {
            DoMantle();
            return;
        }

        HandleInput();

        if (controller.isWallClimbing)
        {
            DoWallClimb();
        }
        else if (!controller.climbKeyConsumed && CanStartWallClimb())
        {
            StartWallClimb();
            controller.climbKeyConsumed = true;
        }
    }

    private void HandleInput()
    {
        if (climbToggle)
        {
            if (Input.GetKeyDown(climbKey))
            {
                if (controller.isWallClimbing)
                {
                    StopWallClimb();
                    controller.climbKeyConsumed = true;
                    return;
                }

                controller.isClimbKeyPressed = IsWallInFront();
                if (controller.isClimbKeyPressed)
                    controller.climbKeyConsumed = false;
            }
        }
        else
        {
            controller.isClimbKeyPressed = Input.GetKey(climbKey);
            if (controller.isClimbKeyPressed)
                controller.climbKeyConsumed = false;
        }
    }

    private bool IsWallInFront()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * controller.playerCharacterController.height * 0.5f;
        return Physics.Raycast(rayOrigin, transform.forward, out _, wallCheckDistance, wallMask);
    }

    private bool CanStartWallClimb()
    {
        if (controller.isWallRunning || controller.isDashing || !controller.isClimbKeyPressed)
            return false;

        return IsWallInFront();
    }

    private void StartWallClimb()
    {
        controller.isWallClimbing = true;
        controller.totalVelocity.y = 0f;

        Vector3 rayOrigin = transform.position + Vector3.up * controller.playerCharacterController.height * 0.5f;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, wallCheckDistance, wallMask))
        {
            controller.currentWallNormal = hit.normal;
            controller.transform.rotation = Quaternion.LookRotation(-controller.currentWallNormal, Vector3.up);
        }

        Debug.Log("Started Wall Climb");
    }

    private void DoWallClimb()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * controller.playerCharacterController.height * 0.5f;

        const int rayCount = 9;
        const float arcAngle = 60f;
        float halfArc = arcAngle / 2f;

        RaycastHit bestHit = new RaycastHit();
        bool foundWall = false;
        float bestDot = -1f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfArc, halfArc, i / (float)(rayCount - 1));
            Vector3 rayDir = Quaternion.Euler(0, angle, 0) * transform.forward;

            Debug.DrawRay(rayOrigin, rayDir * wallCheckDistance, Color.cyan);

            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, wallCheckDistance, wallMask))
            {
                float wallAngle = Vector3.Angle(Vector3.up, hit.normal);
                if (wallAngle > (90f - maxClimbAngle) && wallAngle < (90f + maxClimbAngle))
                {
                    float dot = Vector3.Dot(-hit.normal, transform.forward);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestHit = hit;
                        foundWall = true;
                    }
                }
            }
        }

        if (!foundWall)
        {
            StopWallClimb();
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float sideAngle = horizontalInput > 0 ? 60f : -60f;
            Vector3 sideDir = Quaternion.Euler(0, sideAngle, 0) * transform.forward;

            if (Physics.Raycast(rayOrigin, sideDir, out RaycastHit sideHit, wallCheckDistance, wallMask))
            {
                bestHit = sideHit;
                foundWall = true;
            }
        }

        controller.currentWallNormal = Vector3.Slerp(controller.currentWallNormal, bestHit.normal, Time.deltaTime * 10f);
        Quaternion targetRotation = Quaternion.LookRotation(-controller.currentWallNormal, Vector3.up);
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, targetRotation, Time.deltaTime * 10f);

        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 wallRight = Vector3.Cross(Vector3.up, controller.currentWallNormal).normalized;
        Vector3 climbMovement = -wallRight * horizontalInput * horizontalClimbSpeed + Vector3.up * verticalInput * verticalClimbSpeed;
        Vector3 stickForce = -controller.currentWallNormal * wallStickForce;

        controller.playerCharacterController.Move((climbMovement + stickForce) * Time.deltaTime);
        controller.totalVelocity.y = 0f;

        Vector3 topCheckOrigin = transform.position + Vector3.up * (controller.playerCharacterController.height + mantleDistance);
        bool wallAbove = Physics.Raycast(topCheckOrigin, transform.forward, wallCheckDistance, wallMask);

        if (!wallAbove && verticalInput > 0.1f)
        {
            StartMantle(topCheckOrigin);
        }
    }

    private void StartMantle(Vector3 topCheckOrigin)
    {
        controller.wallClimbMantleStartPos = transform.position;
        controller.wallClimbMantleTargetPos = topCheckOrigin + transform.forward * wallCheckDistance;
        controller.isMantling = true;
        controller.wallClimbMantleTimer = 0f;
        controller.isWallClimbing = false;
    }

    private void DoMantle()
    {
        controller.wallClimbMantleTimer += Time.deltaTime;
        float t = Mathf.Clamp01(controller.wallClimbMantleTimer / mantleDuration);
        t = t * t * (3f - 2f * t);

        Vector3 newPos = Vector3.Lerp(controller.wallClimbMantleStartPos, controller.wallClimbMantleTargetPos, t);
        controller.playerCharacterController.Move(newPos - transform.position);

        if (t >= 1f)
        {
            controller.isMantling = false;
            controller.totalVelocity.y = 0f;
        }
    }

    private void StopWallClimb()
    {
        controller.isWallClimbing = false;
        controller.isClimbKeyPressed = false;
        controller.climbKeyConsumed = true;
    }
}