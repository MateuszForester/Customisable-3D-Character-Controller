using UnityEngine;

public class JumpModule : MonoBehaviour
{
    [Header("Jumping Settings")]
    public float jumpHeight = 15f;
    public bool allowAirControl = true; 
    public bool allowMultiJump = true; 
    public bool omniDirectionalJump = true; 
    public int maxAdditionalJumpCount = 2;
    private int currentJumpCount = 0;

    private ModularCharacterControllerScript controller;
    private CameraModule cameraModuleScript;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void Jump()
    {
        bool grounded = controller.IsGrounded();
        if (grounded) currentJumpCount = 0;

        if (Input.GetButtonDown("Jump"))
        {
            if (grounded || (allowMultiJump && currentJumpCount < maxAdditionalJumpCount))
            {
                controller.velocity.y = Mathf.Sqrt(jumpHeight * 2f * controller.gravity);

                if (!grounded && omniDirectionalJump)
                {
                    float h = 0f, v = 0f;
                    if (Input.GetKey(KeyCode.W)) v += 1f;
                    if (Input.GetKey(KeyCode.S)) v -= 1f;
                    if (Input.GetKey(KeyCode.A)) h -= 1f;
                    if (Input.GetKey(KeyCode.D)) h += 1f;

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

                    if (inputDir.sqrMagnitude > 0.001f)
                    {
                        inputDir.Normalize();
                        controller.horizontalVelocity = inputDir * controller.baseMovementSpeed;
                    }
                }

                currentJumpCount++;
            }
        }
    }
}