using UnityEngine;

public class VRUIFollower : MonoBehaviour
{
    [Header("Target & Positioning")]
    [Tooltip("Takip edilecek ana kamera (Main Camera)")]
    public Transform targetCamera;

    [Tooltip("UI'ýn kameradan ne kadar uzakta duracaðý")]
    public float distance = 2.0f;

    [Header("Smooth Settings")]
    [Tooltip("Pozisyon takip hýzý")]
    public float positionSmoothSpeed = 5.0f;

    [Tooltip("Rotasyon (bakýþ) takip hýzý")]
    public float rotationSmoothSpeed = 5.0f;

    [Header("Pitch Control")]
    [Tooltip("UI kameranýn yukarý/aþaðý eðimini takip etsin mi? False olursa sadece yatayda döner.")]
    public bool followPitch = true;

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Hedef Pozisyon Hesabý
        Vector3 targetPosition = targetCamera.position + (targetCamera.forward * distance);

        // 2. Hedef Rotasyon Hesabý (Kameraya bakýþ açýsý)
        Vector3 lookDirection = targetPosition - targetCamera.position;
        if (!followPitch)
        {
            lookDirection.y = 0; // Sadece Y aksýnda dönmesini saðlar
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // 3. Smooth Pozisyon ve Rotasyon Geçiþi (Süzülme)
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothSpeed), Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed));
    }
}