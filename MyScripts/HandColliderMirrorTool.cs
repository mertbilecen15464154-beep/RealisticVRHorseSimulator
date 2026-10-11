using UnityEngine;
using UnityEditor;

public class HandColliderMirrorTool : EditorWindow
{
    public Transform rightHandRoot;
    public Transform leftHandRoot;

    [MenuItem("Tools/Mirror Hand Colliders")]
    public static void ShowWindow()
    {
        GetWindow<HandColliderMirrorTool>("Mirror Hand Colliders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Hand Collider Mirror Tool", EditorStyles.boldLabel);

        rightHandRoot = (Transform)EditorGUILayout.ObjectField("Right Hand Root", rightHandRoot, typeof(Transform), true);
        leftHandRoot = (Transform)EditorGUILayout.ObjectField("Left Hand Root", leftHandRoot, typeof(Transform), true);

        if (GUILayout.Button("Mirror Colliders Right -> Left"))
        {
            if (rightHandRoot == null || leftHandRoot == null)
            {
                Debug.LogError("Lütfen hem Sað hem de Sol el root objelerini atayýn!");
                return;
            }

            MirrorColliders();
        }
    }

    private void MirrorColliders()
    {
        // Sað eldeki tüm collider objelerini bul
        Collider[] rightColliders = rightHandRoot.GetComponentsInChildren<Collider>();
        int createdCount = 0;

        foreach (Collider rightCol in rightColliders)
        {
            Transform rightBone = rightCol.transform.parent;
            if (rightBone == null) continue;

            // Kemiðin ismindeki '_r_' veya 'right' ifadesini '_l_' veya 'left' yap
            string leftBoneName = rightBone.name.Replace("_r_", "_l_").Replace("_r", "_l").Replace("Right", "Left");
            Transform leftBone = FindChildRecursive(leftHandRoot, leftBoneName);

            if (leftBone != null)
            {
                // Sol kemikte ayný isimde collider var mý kontrol et
                string mirroredColName = rightCol.gameObject.name.Replace("_r_", "_l_").Replace("_r", "_l").Replace("Right", "Left");
                Transform existingCol = leftBone.Find(mirroredColName);

                GameObject leftColObj;
                if (existingCol != null)
                {
                    leftColObj = existingCol.gameObject;
                }
                else
                {
                    leftColObj = new GameObject(mirroredColName);
                    leftColObj.transform.SetParent(leftBone);
                }

                // Transform Ayna Hesabý (Position X ve Rotation Y, Z iþaret deðiþimi)
                Vector3 pos = rightCol.transform.localPosition;
                pos.x = -pos.x; // Position X tersine çevrilir

                Vector3 rot = rightCol.transform.localEulerAngles;
                rot.y = -rot.y; // Y rotasyonu tersine çevrilir
                rot.z = -rot.z; // Z rotasyonu tersine çevrilir

                leftColObj.transform.localPosition = pos;
                leftColObj.transform.localEulerAngles = rot;
                leftColObj.transform.localScale = rightCol.transform.localScale;

                // Capsule Collider kopyalama
                if (rightCol is CapsuleCollider srcCapsule)
                {
                    if (!leftColObj.TryGetComponent<CapsuleCollider>(out var dstCapsule)) dstCapsule = leftColObj.AddComponent<CapsuleCollider>();
                    if (dstCapsule == null) dstCapsule = leftColObj.AddComponent<CapsuleCollider>();

                    Vector3 center = srcCapsule.center;
                    center.x = -center.x;

                    dstCapsule.center = center;
                    dstCapsule.radius = srcCapsule.radius;
                    dstCapsule.height = srcCapsule.height;
                    dstCapsule.direction = srcCapsule.direction;
                    dstCapsule.isTrigger = srcCapsule.isTrigger;
                }
                // Box Collider kopyalama
                else if (rightCol is BoxCollider srcBox)
                {
                    if (!leftColObj.TryGetComponent<BoxCollider>(out var dstBox)) dstBox = leftColObj.AddComponent<BoxCollider>();

                    Vector3 center = srcBox.center;
                    center.x = -center.x;

                    dstBox.center = center;
                    dstBox.size = srcBox.size;
                    dstBox.isTrigger = srcBox.isTrigger;
                }

                createdCount++;
            }
            else
            {
                Debug.LogWarning($"Sol elde karþýlýk gelen kemik bulunamadý: {leftBoneName}");
            }
        }

        Debug.Log($"Ýþlem tamamlandý! {createdCount} adet collider sol ele aynalandý.");
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }

        return null;
    }
}