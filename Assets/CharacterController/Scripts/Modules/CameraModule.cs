using UnityEngine;

public class CameraModule : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float thirdPersonMinDistance = 15f;
    public float thirdPersonMaxDistance = 80f;
    public float thirdPersonScrollSensitivity = 10f;
    public float thirdPersonCollisionBuffer = 0.2f; // distance to keep from colliders
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
        playerCameraFirstPerson.gameObject.SetActive(true);
        playerCameraThirdPerson.gameObject.SetActive(false);
    }

    public void ActivateThirdPersonCamera()
    {
        playerCameraFirstPerson.gameObject.SetActive(false);
        playerCameraThirdPerson.gameObject.SetActive(true);
    }

    public void MouseLook()
    {
        if (!controller.cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (controller.firstPersonActive)
        {
            // First-person: vertical rotation
            controller.xRotation -= mouseY;
            controller.xRotation = Mathf.Clamp(controller.xRotation, -90f, 90f);
            playerCameraFirstPerson.localRotation = Quaternion.Euler(controller.xRotation, 0f, 0f);

            // Rotate character with camera
            controller.transform.Rotate(Vector3.up * mouseX);
        }
        else
        {
            OrbitCamera(mouseX, mouseY);
        }
    }

    public void OrbitCamera(float mouseX, float mouseY)
    {
        if (controller.firstPersonActive) return;

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

        Vector3 pivotPos = playerCameraThirdPerson.parent.Find("CameraPivot").position;

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