using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabHandPose : MonoBehaviour
{
    [Header("References")]
    public XRToggleGrabInteractable interactor;
    public AnimateHandOnInput handAnimatorScript;

    private Animator animator;

    // Varsayýlan pozisyon ve rotasyonu saklamak için
    private Vector3 startingHandPosition;
    private Quaternion startingHandRotation;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (interactor == null)
            interactor = GetComponentInParent<XRToggleGrabInteractable>();

        if (handAnimatorScript == null)
            handAnimatorScript = GetComponent<AnimateHandOnInput>();
    }

    private void OnEnable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.AddListener(SetupPose);
            interactor.selectExited.AddListener(UnsetPose);
        }
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(SetupPose);
            interactor.selectExited.RemoveListener(UnsetPose);
        }
    }

    // Obje tutulduðunda çalýþýr
    public void SetupPose(SelectEnterEventArgs args)
    {
        // Tutulan objede HandData var mý kontrol et
        
        if (args.interactableObject.transform.TryGetComponent<HandData>(out var handData))
        {
            // 1. Normal Input tabanlý animasyon okumasýný kapat
            if (handAnimatorScript != null)
            {
                handAnimatorScript.enabled = false;
            }

            // 2. Pose deðerlerini Animator'a zorunlu uygula
            if (animator != null)
            {
                animator.SetFloat("Grip", handData.gripValue);
                animator.SetFloat("Trigger", handData.pinchValue);
            }

            // 3. Eðer Attach Point tanýmlýysa el modelini objenin tutma noktasýna hizala
            if (handData.attachPoint != null)
            {
                startingHandPosition = transform.localPosition;
                startingHandRotation = transform.localRotation;

                SetHandPositionAndRotation(handData);
            }
        }
    }

    // Obje býrakýldýðýnda çalýþýr
    public void UnsetPose(SelectExitEventArgs args)
    {
        
        if (args.interactableObject.transform.TryGetComponent<HandData>(out var handData))
        {
            // 1. Normal Input tabanlý animasyon okumasýný tekrar aç
            if (handAnimatorScript != null)
            {
                handAnimatorScript.enabled = true;
            }

            // 2. Eli eski yerel pozisyonuna ve rotasyonuna döndür
            if (handData.attachPoint != null)
            {
                transform.SetLocalPositionAndRotation(startingHandPosition, startingHandRotation);
            }
        }
    }

    private void SetHandPositionAndRotation(HandData handData)
    {
        transform.SetPositionAndRotation(handData.attachPoint.position, handData.attachPoint.rotation);
    }
}