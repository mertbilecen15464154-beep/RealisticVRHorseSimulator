using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<HorseControllerVR>() != null)
        {
            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.OnCheckpointPassed(checkpointIndex);
            }
        }
    }
}