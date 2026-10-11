using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
public class VRDynamicCrouch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Height Settings")]
    [Tooltip("Karakterin minimum kapsül yüksekliði (tam çömelme)")]
    [SerializeField] private float minHeight = 0.6f;

    [Tooltip("Karakterin maksimum kapsül yüksekliði")]
    [SerializeField] private float maxHeight = 2.0f;

    [Tooltip("Kapsül tabanýnýn yere olan ofseti")]
    [SerializeField] private float heightPadding = 0.1f;

    private CharacterController characterController;
    private Transform cameraTransform;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (xrOrigin == null)
            xrOrigin = GetComponentInParent<XROrigin>();
    }

    private void Start()
    {
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            cameraTransform = xrOrigin.Camera.transform;
        }
    }

    private void Update()
    {
        // CharacterController pasifken (örneðin ata binildiðinde) çalýþmasýn
        if (!characterController.enabled || cameraTransform == null)
            return;

        UpdateColliderHeight();
    }

    private void UpdateColliderHeight()
    {
        // Kameranýn XR Origin içindeki yerel Y yüksekliðini al
        float currentCameraHeight = cameraTransform.localPosition.y;

        // Kapsül yüksekliðini belirle ve sýnýrla
        float targetHeight = Mathf.Clamp(currentCameraHeight + heightPadding, minHeight, maxHeight);

        // CharacterController yüksekliðini ve merkezini güncelle
        characterController.height = targetHeight;
        characterController.center = new Vector3(
            cameraTransform.localPosition.x,
            targetHeight / 2f,
            cameraTransform.localPosition.z
        );
    }
}