using UnityEngine;

public class HandData : MonoBehaviour
{
    public enum HandModelType { Left, Right }

    [Header("Hand Pose Settings")]
    public HandModelType handType;

    [Header("Attach Point (El Tutma Noktasý)")]
    public Transform attachPoint;

    [Header("Animator Parametreleri (Grip / Trigger)")]
    [Range(0, 1)] public float gripValue = 1f;   // Dizgin için 1 (Yumruk)
    [Range(0, 1)] public float pinchValue = 0f;  // Trigger deðeri

    [Header("Kemik Pozisyonlandýrma (Ýsteðe Baðlý)")]
    public Transform root;
    public Transform[] fingerBones;
}