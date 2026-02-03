using UnityEngine;

public class CameraModule : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float thirdPersonMinDistance = 15f;
    public float thirdPersonMaxDistance = 80f;
    public float thirdPersonZoomAdjustmentSensitivity = 10f;
    public float thirdPersonCollisionBuffer = 0.2f;
    public Transform playerCameraFirstPerson;
    public Transform playerCameraThirdPerson;
    public LayerMask thirdPersonCameraCollisionMask;

    private ModularCharacterControllerScript controller;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void CameraSwitch()
    {
        if (Input.GetKeyDown(controller.switchCameraKey))
        {
            controller.firstPersonActive = !controller.firstPersonActive;
            if (controller.firstPersonActive) ActivateFirstPersonCamera();
            else ActivateThirdPersonCamera();
        }
    }

    public void ActivateFirstPersonCamera()
    {
        if (playerCameraFirstPerson != null) playerCameraFirstPerson.gameObject.SetActive(true);
        if (playerCameraThirdPerson != null) playerCameraThirdPerson.gameObject.SetActive(false);
    }

    public void ActivateThirdPersonCamera()
    {
        if (playerCameraFirstPerson != null) playerCameraFirstPerson.gameObject.SetActive(false);
        if (playerCameraThirdPerson != null) playerCameraThirdPerson.gameObject.SetActive(true);
    }

    public void MouseLook()
    {
        if (!controller.cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (controller.isWallRunning)
        {
            if (controller.firstPersonActive && playerCameraFirstPerson != null)
            {
                controller.firstPersonCameraPitch -= mouseY;
                controller.firstPersonCameraPitch = Mathf.Clamp(controller.firstPersonCameraPitch, -90f, 90f);
                playerCameraFirstPerson.localRotation = Quaternion.Euler(controller.firstPersonCameraPitch, 0f, 0f);
            }
            else if (!controller.firstPersonActive && playerCameraThirdPerson != null)
            {
                OrbitCamera(mouseX, mouseY, true);
            }
            return;
        }

        if (controller.isWallClimbing)
        {
            if (controller.firstPersonActive && playerCameraFirstPerson != null)
            {
                controller.firstPersonCameraPitch -= mouseY;
                controller.firstPersonCameraPitch = Mathf.Clamp(controller.firstPersonCameraPitch, -90f, 90f);
                playerCameraFirstPerson.localRotation = Quaternion.Euler(controller.firstPersonCameraPitch, 0f, 0f);
            }
            else if (!controller.firstPersonActive && playerCameraThirdPerson != null)
            {
                OrbitCamera(mouseX, mouseY, true);
            }
            return;
        }

        if (controller.firstPersonActive && playerCameraFirstPerson != null)
        {
            controller.firstPersonCameraPitch -= mouseY;
            controller.firstPersonCameraPitch = Mathf.Clamp(controller.firstPersonCameraPitch, -90f, 90f);
            playerCameraFirstPerson.localRotation = Quaternion.Euler(controller.firstPersonCameraPitch, 0f, 0f);
            controller.transform.Rotate(Vector3.up * mouseX);
            return;
        }

        if (!controller.firstPersonActive)
        {
            OrbitCamera(mouseX, mouseY);
        }
    }

    public void OrbitCamera(float mouseX, float mouseY, bool disablePlayerRotation = false)
    {
        if (controller.firstPersonActive) return;

        controller.thirdPersonCameraYaw += mouseX;
        controller.thirdPersonCameraPitch -= mouseY;
        controller.thirdPersonCameraPitch = Mathf.Clamp(controller.thirdPersonCameraPitch, -80f, 80f);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        controller.thirdPersonCameraTargetDistance -= scroll * thirdPersonZoomAdjustmentSensitivity;
        controller.thirdPersonCameraTargetDistance = Mathf.Clamp(controller.thirdPersonCameraTargetDistance, thirdPersonMinDistance, thirdPersonMaxDistance);

        if (!disablePlayerRotation)
        {
            Vector3 camForward = Quaternion.Euler(0f, controller.thirdPersonCameraYaw, 0f) * Vector3.forward;
            float rotationSpeed = 10f;
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation,
                                                            Quaternion.LookRotation(camForward),
                                                            rotationSpeed * Time.deltaTime);
        }

        if (playerCameraThirdPerson == null || playerCameraThirdPerson.parent == null) return;
        Transform cameraPivot = playerCameraThirdPerson.parent.Find("CameraPivot");
        if (cameraPivot == null) return;

        Vector3 pivotPos = cameraPivot.position;
        Vector3 direction = Quaternion.Euler(controller.thirdPersonCameraPitch, controller.thirdPersonCameraYaw, 0f) * Vector3.forward;
        Vector3 desiredPos = pivotPos - direction * controller.thirdPersonCameraTargetDistance;

        RaycastHit hit;
        float collisionRadius = 0.2f;
        if (Physics.SphereCast(pivotPos, collisionRadius, (desiredPos - pivotPos).normalized,
                               out hit, controller.thirdPersonCameraTargetDistance, thirdPersonCameraCollisionMask))
        {
            controller.thirdPersonCameraCurrentDistance = Mathf.Clamp(hit.distance - thirdPersonCollisionBuffer, thirdPersonMinDistance, thirdPersonMaxDistance);
            desiredPos = pivotPos - direction * controller.thirdPersonCameraCurrentDistance;
        }
        else
        {
            controller.thirdPersonCameraCurrentDistance = controller.thirdPersonCameraTargetDistance;
        }

        playerCameraThirdPerson.position = desiredPos;
        playerCameraThirdPerson.LookAt(pivotPos);
    }
}