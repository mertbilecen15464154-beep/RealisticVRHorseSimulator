using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class VRRecenterHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Input Settings")]
    [Tooltip("Sýfýrlama yapmak istediðin tuþ")]
    [SerializeField] private InputActionProperty recenterAction;

    private void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = GetComponentInParent<XROrigin>();
    }

    private void OnEnable()
    {
        if (recenterAction.action != null)
        {
            recenterAction.action.Enable();
            recenterAction.action.performed += OnRecenterPressed;
        }
    }

    private void OnDisable()
    {
        if (recenterAction.action != null)
        {
            recenterAction.action.performed -= OnRecenterPressed;
            recenterAction.action.Disable();
        }
    }

    private void OnRecenterPressed(InputAction.CallbackContext context)
    {
        RecenterView();
    }

    /// <summary>
    /// Görünümü, Bakýþ Alanýný ve Yüksekliði tam o anki konuma sýfýrlar.
    /// </summary>
    public void RecenterView()
    {
        if (xrOrigin == null || xrOrigin.Camera == null) return;

        // 1. Subsystem seviyesinde bakýþ yönünü ve XZ alanýný sýfýrla
        var inputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputSubsystems);
        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem != null && subsystem.running)
            {
                subsystem.TryRecenter();
            }
        }

        // 2. Yükseklik Offset Hesabý (Oculus Sýfýrlama Mantýðý)
        // Oyuncunun o anki kafa yüksekliðini alýp CameraFloorOffsetObject'i kaydýrýyoruz
        Transform cameraOffsetTransform = xrOrigin.CameraFloorOffsetObject != null
            ? xrOrigin.CameraFloorOffsetObject.transform
            : xrOrigin.transform;

        // Oyuncu oturuyorsa veya çömelmiþse, þu anki kafa yüksekliðini varsayýlan yüksekliðe çekiyoruz
        float currentCamLocalY = xrOrigin.Camera.transform.localPosition.y;
        float desiredYOffset = xrOrigin.CameraYOffset - currentCamLocalY;

        Vector3 currentOffsetPos = cameraOffsetTransform.localPosition;
        currentOffsetPos.y = desiredYOffset;
        cameraOffsetTransform.localPosition = currentOffsetPos;
    }
}