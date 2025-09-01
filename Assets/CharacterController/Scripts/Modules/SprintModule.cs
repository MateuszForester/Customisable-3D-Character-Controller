using UnityEngine;

public class SprintModule : MonoBehaviour
{
    [Header("Sprint Settings")]
    public float sprintSpeed = 75f;
    public bool sprintToggle = false;
    public KeyCode sprintKey = KeyCode.LeftShift;

    private ModularCharacterControllerScript controller;
    private CrouchModule crouchModule;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void Sprint()
    {
        if (sprintToggle)
        {
            if (Input.GetKeyDown(sprintKey))
            {
                ToggleSprint();
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
        // Optional: disable crouch when sprinting
        if (controller.isSprinting)
            crouchModule.SetCrouch(false);
    }

    public void SetSprint(bool sprint)
    {
        controller.isSprinting = sprint;
    }
}