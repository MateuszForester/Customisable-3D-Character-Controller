using UnityEngine;

public class CrouchModule : MonoBehaviour
{
    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 15f;
    public bool crouchToggle = false;

    private ModularCharacterControllerScript controller;

    private float originalHeight;
    private Vector3 originalCenter;

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;

        if (controller != null && controller.playerCharacterController != null)
        {
            originalHeight = controller.playerCharacterController.height;
            originalCenter = controller.playerCharacterController.center;

            if (standingHeight <= 0f) standingHeight = originalHeight;
        }
    }

    public void Crouch()
    {
        if (controller == null || controller.playerCharacterController == null) return;

        if (crouchToggle)
        {
            if (Input.GetKeyDown(controller.crouchKey))
                ToggleCrouch();
        }
        else
        {
            SetCrouch(Input.GetKey(controller.crouchKey));
        }
    }

    public void ToggleCrouch()
    {
        SetCrouch(!controller.isCrouching);
    }

    public void SetCrouch(bool crouch)
    {
        if (controller == null || controller.playerCharacterController == null) return;

        controller.isCrouching = crouch;

        float targetHeight = crouch ? crouchHeight : standingHeight;

        var cc = controller.playerCharacterController;

        float bottomWorldY = controller.transform.position.y + cc.center.y - (cc.height * 0.5f);

        cc.height = targetHeight;

        Vector3 c = cc.center;
        c.y = (bottomWorldY - controller.transform.position.y) + (cc.height * 0.5f);
        cc.center = c;
    }
}
