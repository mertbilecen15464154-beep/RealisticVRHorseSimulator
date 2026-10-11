using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    [Header("Input Action References")]
    public InputActionProperty pinchAnimationAction;
    public InputActionProperty gripAnimationAction;

    [Header("Animator Reference")]
    public Animator handAnimator;

    private void Update()
    {
        // Pinch (Tetik / Ýþaret Parmaðý) Deðeri
        float triggerValue = pinchAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Trigger", triggerValue);

        // Grip (Kavrama / Orta Parmaðýný Sýkma) Deðeri
        float gripValue = gripAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Grip", gripValue);
    }
}