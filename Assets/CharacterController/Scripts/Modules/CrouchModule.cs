using UnityEngine;

public class CrouchModule : MonoBehaviour
{
    [Header("Crouch Settings")]
    public float crouchHeight = 2f;
    public float standingHeight = 2f;
    public float crouchSpeed = 15f;
    public bool crouchToggle = false;
    public KeyCode crouchKey = KeyCode.LeftControl;

    private ModularCharacterControllerScript controller;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void Crouch()
    {
        if (crouchToggle)
        {
            if (Input.GetKeyDown(crouchKey))
            {
                ToggleCrouch();
            }
        }
        else
        {
            if (Input.GetKey(crouchKey))
            {
                SetCrouch(true);
            }
            else
            {
                SetCrouch(false);
            }
        }
    }

    public void ToggleCrouch()
    {
        controller.isCrouching = !controller.isCrouching;
        SetCrouch(controller.isCrouching);
    }

    public void SetCrouch(bool crouch)
    {
        controller.isCrouching = crouch;
        controller.playerCharacterController.height = crouch ? crouchHeight : standingHeight;
    }
}