using System.Collections;
using UnityEngine;

public class WallClimbModule : MonoBehaviour
{
    [Header("Wall Climb Settings")]
    public float wallCheckDistance = 7f;
    public LayerMask wallMask;
    public LayerMask groundMask;
    public float maxClimbAngle = 45f;
    public float wallStickForce = 30f;
    public bool climbToggle = true;
    public float horizontalClimbSpeed = 25f;
    public float verticalClimbSpeed = 25f;
    public float cornerTurnSpeed = 15f;

    [Header("Mantle Settings")]
    public float mantleDistance = 1f;
    public float mantleDuration = 0.4f;

    [Header("Corner Commitment")]
    public float cornerCommitDuration = 0.18f;
    public float cornerSwitchCooldown = 0.12f;
    public float cornerNormalDifferenceThreshold = 0.85f;
    public float cornerPullStrength = 30f;
    public float cornerPullMax = 6f;
    public float wallDistanceExtraOffset = 0.02f;

    [Header("Outside Corner Snap")]
    public float seamSlideStrength = 25f;
    public float seamSlideMax = 6f;

    private ModularCharacterControllerScript controller;
    private bool mantleControllerWasEnabled;

    private Vector3 committedNormal;
    private float commitTimer;
    private float switchCooldownTimer;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void WallClimbUpdate()
    {
        if (controller == null || controller.playerCharacterController == null)
            return;

        if (commitTimer > 0f) commitTimer -= Time.deltaTime;
        if (switchCooldownTimer > 0f) switchCooldownTimer -= Time.deltaTime;

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
            if (Input.GetKeyDown(controller.climbKey))
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
            controller.isClimbKeyPressed = Input.GetKey(controller.climbKey);
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
            committedNormal = controller.currentWallNormal;
            commitTimer = 0f;
            switchCooldownTimer = 0f;
        }
    }

    private void DoWallClimb()
    {
        CharacterController cc = controller.playerCharacterController;
        Vector3 rayOrigin = transform.position + Vector3.up * cc.height * 0.5f;

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
        float verticalInput = Input.GetAxisRaw("Vertical");

        bool hasSideHit = false;
        RaycastHit sideHit = new RaycastHit();

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float sideAngle = horizontalInput > 0 ? 60f : -60f;
            Vector3 sideDir = Quaternion.Euler(0, sideAngle, 0) * transform.forward;

            if (Physics.Raycast(rayOrigin, sideDir, out RaycastHit sh, wallCheckDistance, wallMask))
            {
                float wallAngle = Vector3.Angle(Vector3.up, sh.normal);
                if (wallAngle > (90f - maxClimbAngle) && wallAngle < (90f + maxClimbAngle))
                {
                    sideHit = sh;
                    hasSideHit = true;
                }
            }
        }

        RaycastHit chosenHit = bestHit;

        bool cornerCandidate = false;
        if (hasSideHit)
        {
            float nDot = Vector3.Dot(bestHit.normal.normalized, sideHit.normal.normalized);
            if (nDot < cornerNormalDifferenceThreshold)
                cornerCandidate = true;
        }

        if (cornerCandidate)
        {
            if (commitTimer > 0f)
            {
                float dBest = Vector3.Dot(bestHit.normal.normalized, committedNormal.normalized);
                float dSide = Vector3.Dot(sideHit.normal.normalized, committedNormal.normalized);
                chosenHit = (dSide > dBest) ? sideHit : bestHit;
            }
            else
            {
                if (switchCooldownTimer > 0f)
                {
                    float dBest = Vector3.Dot(bestHit.normal.normalized, committedNormal.normalized);
                    float dSide = Vector3.Dot(sideHit.normal.normalized, committedNormal.normalized);
                    chosenHit = (dSide > dBest) ? sideHit : bestHit;
                }
                else
                {
                    if (hasSideHit && Mathf.Abs(horizontalInput) > 0.1f)
                        chosenHit = sideHit;
                    else
                    {
                        float dBest = Vector3.Dot(bestHit.normal.normalized, controller.currentWallNormal.normalized);
                        float dSide = Vector3.Dot(sideHit.normal.normalized, controller.currentWallNormal.normalized);
                        chosenHit = (dSide > dBest) ? sideHit : bestHit;
                    }

                    committedNormal = chosenHit.normal;
                    commitTimer = cornerCommitDuration;
                    switchCooldownTimer = cornerSwitchCooldown;
                }
            }
        }
        else
        {
            committedNormal = chosenHit.normal;
            commitTimer = 0f;
        }

        float turnT = 1f - Mathf.Exp(-cornerTurnSpeed * Time.deltaTime);

        controller.currentWallNormal = Vector3.Slerp(controller.currentWallNormal, chosenHit.normal, turnT);
        Quaternion targetRotation = Quaternion.LookRotation(-controller.currentWallNormal, Vector3.up);
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, targetRotation, turnT);

        Vector3 wallRight = Vector3.Cross(Vector3.up, controller.currentWallNormal).normalized;
        Vector3 climbMovement = -wallRight * horizontalInput * horizontalClimbSpeed + Vector3.up * verticalInput * verticalClimbSpeed;

        Vector3 stickForce = -controller.currentWallNormal * wallStickForce;

        Vector3 cornerPull = Vector3.zero;
        Vector3 seamSlide = Vector3.zero;

        if (cornerCandidate)
        {
            float desired = cc.radius + cc.skinWidth + wallDistanceExtraOffset;
            float error = chosenHit.distance - desired;
            if (error > 0f)
            {
                cornerPull = -chosenHit.normal.normalized * (error * cornerPullStrength);
                if (cornerPull.magnitude > cornerPullMax)
                    cornerPull = cornerPull.normalized * cornerPullMax;
            }

            Vector3 nA = bestHit.normal.normalized;
            Vector3 nB = sideHit.normal.normalized;

            Vector3 seamAxis = Vector3.Cross(nA, nB);
            if (seamAxis.sqrMagnitude > 0.0001f)
            {
                seamAxis.Normalize();
                Vector3 toFaceA = Vector3.Cross(seamAxis, nA).normalized;
                Vector3 toFaceB = Vector3.Cross(seamAxis, nB).normalized;

                Vector3 toChosen = Vector3.Cross(seamAxis, chosenHit.normal.normalized).normalized;

                Vector3 desiredSlide = toChosen;

                if (Mathf.Abs(horizontalInput) > 0.1f)
                {
                    float s = Mathf.Sign(horizontalInput);
                    Vector3 inputDir = (-wallRight * s).normalized;
                    if (Vector3.Dot(desiredSlide, inputDir) < 0f)
                        desiredSlide = -desiredSlide;
                }

                seamSlide = desiredSlide * seamSlideStrength;
                if (seamSlide.magnitude > seamSlideMax)
                    seamSlide = seamSlide.normalized * seamSlideMax;
            }
        }

        controller.playerCharacterController.Move((climbMovement + stickForce + cornerPull + seamSlide) * Time.deltaTime);
        controller.totalVelocity.y = 0f;

        Vector3 topCheckOrigin = transform.position + Vector3.up * (cc.height + mantleDistance);
        bool wallAbove = Physics.Raycast(topCheckOrigin, transform.forward, wallCheckDistance, wallMask);

        if (!wallAbove && verticalInput > 0.1f)
        {
            StartMantle(topCheckOrigin);
        }
    }

    private void StartMantle(Vector3 topCheckOrigin)
    {
        controller.wallClimbMantleStartPos = transform.position;
        controller.wallClimbMantleTargetPos = topCheckOrigin + transform.forward * wallCheckDistance + Vector3.up * 0.1f;

        mantleControllerWasEnabled = controller.playerCharacterController.enabled;
        if (mantleControllerWasEnabled)
            controller.playerCharacterController.enabled = false;

        controller.horizontalVelocity = Vector3.zero;
        controller.totalVelocity = Vector3.zero;

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
        transform.position = newPos;

        if (t >= 1f)
        {
            controller.isMantling = false;
            controller.totalVelocity.y = 0f;

            if (mantleControllerWasEnabled)
            {
                Vector3 safePos = FindSafeReenablePosition(transform.position);
                transform.position = safePos;
                controller.playerCharacterController.enabled = true;
            }
        }
    }

    private Vector3 FindSafeReenablePosition(Vector3 desiredPos)
    {
        CharacterController cc = controller.playerCharacterController;

        float radius = cc.radius;
        float height = Mathf.Max(cc.height, radius * 2f);
        Vector3 center = desiredPos + cc.center;

        float half = height * 0.5f;
        float offset = Mathf.Max(0f, half - radius);

        const int attempts = 6;
        const float step = 0.1f;

        for (int i = 0; i <= attempts; i++)
        {
            Vector3 testPos = desiredPos + Vector3.up * (i * step);
            Vector3 testCenter = testPos + cc.center;
            Vector3 tp1 = testCenter + Vector3.up * offset;
            Vector3 tp2 = testCenter - Vector3.up * offset;

            if (!Physics.CheckCapsule(tp1, tp2, radius, ~0, QueryTriggerInteraction.Ignore))
                return testPos;
        }

        return desiredPos;
    }

    private void StopWallClimb()
    {
        controller.isWallClimbing = false;
        controller.isClimbKeyPressed = false;
        controller.climbKeyConsumed = true;
        commitTimer = 0f;
        switchCooldownTimer = 0f;
    }
}
