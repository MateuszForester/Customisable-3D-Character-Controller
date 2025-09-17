using UnityEngine;

public class SprintModule : MonoBehaviour
{
    [Header("Sprint Settings")]
    public float sprintSpeed = 75f;
    public bool sprintToggle = false;
    public KeyCode sprintKey = KeyCode.LeftShift;

    private ModularCharacterControllerScript controller;
    private CrouchModule crouchModuleScript;

    public void Initialize(ModularCharacterControllerScript characterController, CrouchModule crouchModule)
    {
        controller = characterController;
        crouchModuleScript = crouchModule;
    }

    public void Sprint()
    {
        if (sprintToggle)
        {
            if (Input.GetKeyDown(sprintKey))
            {
                ToggleSprint();
                crouchModuleScript.SetCrouch(false);
            }
        }
        else
        {
            if (Input.GetKey(sprintKey))
            {
                SetSprint(true);
            }
            else
            {
                SetSprint(false);
            }
        }
    }

    public void ToggleSprint()
    {
        controller.isSprinting = !controller.isSprinting;
        if (controller.isSprinting)
            crouchModuleScript.SetCrouch(false);
    }

    public void SetSprint(bool sprint)
    {
        controller.isSprinting = sprint;
    }
}