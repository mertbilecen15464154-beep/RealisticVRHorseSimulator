using UnityEngine;

public class StartLine : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerExit(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<HorseControllerVR>() != null)
        {
            if (RaceManager.Instance != null)
            {
                hasTriggered = true;

                // 1. Geri sayýmý ve yarýþ sürecini baþlat
                RaceManager.Instance.StartRaceWithCountdown();

                // 2. Kapýnýn ötelenerek pistten çekilmesini hemen baþlat
                RaceManager.Instance.TriggerGateMove();

                // 3. StartLine alanla iþi bittiði için kendini yok eder
                Destroy(gameObject);
            }
        }
    }
}