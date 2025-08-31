using UnityEngine;

public class AnimationModule : MonoBehaviour
{
    [Header("Animation Settings")]
    public float transitionDuration = 0.25f; // adjust for smoothness

    private ModularCharacterControllerScript controller;
    private string currentState = "";

    public void Initialize(ModularCharacterControllerScript characterController)
    {
        controller = characterController;
    }

    public void Animations()
    {
        string nextState = "Idle";

        if (controller.isDashing)
        {
            nextState = "Dash";
        }
        else if (!controller.IsGrounded())
        {
            nextState = "Jump";
        }
        else if (controller.isCrouching)
        {
            nextState = (controller.horizontalVelocity.magnitude > 0.1f) ? "CrouchWalk" : "CrouchIdle";
        }
        else if (controller.horizontalVelocity.magnitude > 0.1f)
        {
            nextState = controller.isSprinting ? "Run" : "Walk";
        }

        if (currentState != nextState)
        {
            controller.animator.CrossFadeInFixedTime(nextState, transitionDuration);
            currentState = nextState;
        }
    }
}