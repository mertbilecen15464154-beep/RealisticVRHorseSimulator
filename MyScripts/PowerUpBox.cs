using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; // XRI v3.x için

public class PowerUpBox : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public float speedBoostAmount = 5f;
    public float boostDuration = 5f;

    [Header("Audio & Effects")]
    public AudioClip collectSound;
    public ParticleSystem collectEffect;

    // Trigger alanýna giren XR Controller/Hand etkileþimlerini algýlar
    private void OnTriggerEnter(Collider other)
    {
        // Giren nesnenin üzerinde veya ebeveyninde bir XR Interactor var mý kontrol et
        XRBaseInteractor interactor = other.GetComponentInParent<XRBaseInteractor>();

        if (interactor != null)
        {
            // Interactor'ýn baðlý olduðu oyuncu/at sistemini bul ve bonusu uygula
            HorseControllerVR horse = interactor.GetComponentInParent<HorseControllerVR>();

            if (horse != null)
            {
                ApplyPowerUp(horse);
            }
            else
            {
                // Alternatif olarak scene'deki aktif atý bul
                HorseControllerVR activeHorse = FindAnyObjectByType<HorseControllerVR>();
                if (activeHorse != null && activeHorse.isRiding)
                {
                    ApplyPowerUp(activeHorse);
                }
            }
        }
    }

    private void ApplyPowerUp(HorseControllerVR _)
    {
        // Ses ve efektleri tetikle
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, 1f);
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Kutuyu yok et veya gizle
        Destroy(gameObject);
    }
}