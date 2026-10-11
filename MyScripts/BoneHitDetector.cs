using UnityEngine;

public class BoneHitDetector : MonoBehaviour
{
    public LayerMask targetLayers; // Sadece algýlamak istediðin katmaný seç (örneðin Default veya Environment)
    public Vector3 boxSize = new(0.5f, 0.5f, 0.5f);
    public Vector3 boxOffset = Vector3.zero;

    void Update()
    {
        // 1. Taranacak pozisyon ve rotasyonu hesapla
        Vector3 center = transform.position + transform.TransformDirection(boxOffset);
        Quaternion rotation = transform.rotation;

        // 2. OverlapBox ile hem Trigger hem katý tüm collider'larý tara
        Collider[] hits = Physics.OverlapBox(center, boxSize / 2f, rotation, targetLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            // Atýn kendi üzerindeki collider'larý ve bu kemiðin kendisini algýlamasýn
            if (hit.gameObject != gameObject && !hit.transform.IsChildOf(transform.root))
            {
                Debug.Log($"<color=green>[BAÞARILI]</color> {gameObject.name} kemiði, {hit.name} nesnesini ALGILADI!");
            }
        }
    }

    // Scene ekranýnda yeþil/kýrmýzý tarama kutusunu görselleþtirmek için
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position + transform.TransformDirection(boxOffset), transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}