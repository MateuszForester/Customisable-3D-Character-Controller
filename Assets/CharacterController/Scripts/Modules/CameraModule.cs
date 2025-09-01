using UnityEngine;

public class CameraModule : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float thirdPersonMinDistance = 15f;
    public float thirdPersonMaxDistance = 80f;
    public float thirdPersonScrollSensitivity = 10f;
    public float thirdPersonCollisionBuffer = 0.2f;
    public Transform playerCameraFirstPerson;
    public Transform playerCameraThirdPerson;
    public LayerMask thirdPersonCameraCollisionMask;
    public KeyCode switchCameraKey = KeyCode.V;

    private ModularCharacterControllerScript controller;
    
    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void CameraSwitch()
    {
        if (Input.GetKeyDown(switchCameraKey))
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
            controller.xRotation -= mouseY;
            controller.xRotation = Mathf.Clamp(controller.xRotation, -90f, 90f);

            if (controller.firstPersonActive && playerCameraFirstPerson != null)
            {
                Quaternion targetRotation = Quaternion.Euler(controller.xRotation, controller.transform.eulerAngles.y, 0f);

                Vector3 currentEuler = playerCameraFirstPerson.localRotation.eulerAngles;
                float smoothPitch = Mathf.LerpAngle(currentEuler.x, controller.xRotation, Time.deltaTime * 10f);
                playerCameraFirstPerson.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);

                controller.transform.Rotate(Vector3.up * mouseX);
            }
            else if (!controller.firstPersonActive && playerCameraThirdPerson != null)
            {
                Vector3 lookAtPoint = controller.transform.position + controller.transform.forward;
                Vector3 flatLookDir = (lookAtPoint - playerCameraThirdPerson.position).normalized;
                flatLookDir.y = 0f;
                if (flatLookDir.sqrMagnitude > 0.001f) flatLookDir.Normalize();

                Quaternion targetRotation = Quaternion.LookRotation(flatLookDir, Vector3.up) * Quaternion.Euler(controller.xRotation, 0f, 0f);
                playerCameraThirdPerson.rotation = Quaternion.Slerp(playerCameraThirdPerson.rotation, targetRotation, Time.deltaTime * 10f);
            }
            return;
        }

        if (controller.firstPersonActive && playerCameraFirstPerson != null)
        {
            controller.xRotation -= mouseY;
            controller.xRotation = Mathf.Clamp(controller.xRotation, -90f, 90f);
            playerCameraFirstPerson.localRotation = Quaternion.Euler(controller.xRotation, 0f, 0f);
            controller.transform.Rotate(Vector3.up * mouseX);
            return;
        }

        if (!controller.firstPersonActive)
        {
            OrbitCamera(mouseX, mouseY);
        }
    }

    public void OrbitCamera(float mouseX, float mouseY)
    {
        if (controller.firstPersonActive || controller.isWallRunning) return;

        controller.thirdPersonYaw += mouseX;
        controller.thirdPersonPitch -= mouseY;
        controller.thirdPersonPitch = Mathf.Clamp(controller.thirdPersonPitch, -80f, 80f);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        controller.thirdPersonTargetDistance -= scroll * thirdPersonScrollSensitivity;
        controller.thirdPersonTargetDistance = Mathf.Clamp(controller.thirdPersonTargetDistance, thirdPersonMinDistance, thirdPersonMaxDistance);

        Vector3 camForward = Quaternion.Euler(0f, controller.thirdPersonYaw, 0f) * Vector3.forward;
        float rotationSpeed = 10f;
        controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation,
                                                        Quaternion.LookRotation(camForward),
                                                        rotationSpeed * Time.deltaTime);

        if (playerCameraThirdPerson == null || playerCameraThirdPerson.parent == null) return;
        Transform cameraPivot = playerCameraThirdPerson.parent.Find("CameraPivot");
        if (cameraPivot == null) return;

        Vector3 pivotPos = cameraPivot.position;
        Vector3 direction = Quaternion.Euler(controller.thirdPersonPitch, controller.thirdPersonYaw, 0f) * Vector3.forward;
        Vector3 desiredPos = pivotPos - direction * controller.thirdPersonTargetDistance;

        RaycastHit hit;
        float collisionRadius = 0.2f;
        if (Physics.SphereCast(pivotPos, collisionRadius, (desiredPos - pivotPos).normalized,
                               out hit, controller.thirdPersonTargetDistance, thirdPersonCameraCollisionMask))
        {
            controller.thirdPersonCurrentDistance = Mathf.Clamp(hit.distance - thirdPersonCollisionBuffer, thirdPersonMinDistance, thirdPersonMaxDistance);
            desiredPos = pivotPos - direction * controller.thirdPersonCurrentDistance;
        }
        else
        {
            controller.thirdPersonCurrentDistance = controller.thirdPersonTargetDistance;
        }

        playerCameraThirdPerson.position = desiredPos;
        playerCameraThirdPerson.LookAt(pivotPos);
    }
}
