using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandCollisionHandler : MonoBehaviour
{
    [Header("Collision Layers")]
    [Tooltip("Elin temas ettiðinde etkileþime geçeceði katmanlar")]
    public LayerMask collisionLayers;

    [Header("Haptic Feedback (Titreþim)")]
    public bool enableHaptics = true;
    [Range(0f, 1f)] public float hapticIntensity = 0.5f;
    public float hapticDuration = 0.1f;

    [Header("Audio Feedback")]
    public AudioSource handAudioSource;
    public AudioClip impactSound;

    private XRDirectInteractor interactor;
    private Collider[] myHandColliders;

    private void Awake()
    {
        interactor = GetComponentInParent<XRDirectInteractor>();

        // 1. Elin kendisindeki ve parmaklarýndaki tüm Collider'larý topla
        myHandColliders = GetComponentsInChildren<Collider>();

        // 2. Ayný elin kendi parmak/el collider'larýnýn birbirini tetiklemesini engelle
        for (int i = 0; i < myHandColliders.Length; i++)
        {
            for (int j = i + 1; j < myHandColliders.Length; j++)
            {
                Physics.IgnoreCollision(myHandColliders[i], myHandColliders[j], true);
            }
        }

        // Rigidbody Kontrolü
        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Temas eden collider bu elin kendi collider'larýndan biriyse iþlem yapma
        if (IsSelfCollider(other)) return;

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Temas eden collider bu elin kendi collider'larýndan biriyse iþlem yapma
        if (IsSelfCollider(collision.collider)) return;

        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(collision.gameObject);
        }
    }

    private bool IsSelfCollider(Collider col)
    {
        if (myHandColliders == null) return false;
        for (int i = 0; i < myHandColliders.Length; i++)
        {
            if (myHandColliders[i] == col) return true;
        }
        return false;
    }

    private void HandleImpact(GameObject _)
    {
        // 1. Titreþim (Haptics)
        if (enableHaptics && interactor != null)
        {
            interactor.SendHapticImpulse(hapticIntensity, hapticDuration);
        }

        // 2. Ses Efekti
        if (handAudioSource != null && impactSound != null)
        {
            handAudioSource.PlayOneShot(impactSound);
        }
    }
}