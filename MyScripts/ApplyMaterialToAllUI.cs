using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class ApplyMaterialToAllUI : EditorWindow
{
    private Material targetMaterial;

    [MenuItem("Tools/UI Overlay Assigner")]
    public static void ShowWindow()
    {
        GetWindow<ApplyMaterialToAllUI>("UI Overlay Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tüm Image UI'larýna Materyal Ata", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Uygulanacak materyal seçimi
        targetMaterial = (Material)EditorGUILayout.ObjectField("Atanacak Materyal", targetMaterial, typeof(Material), false);

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Sahnede Bulunan Tüm Image'lara Uygula", GUILayout.Height(35)))
        {
            if (targetMaterial == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen önce bir Materyal seçin!", "Tamam");
                return;
            }

            ApplyMaterialToSceneImages();
        }
    }

    private void ApplyMaterialToSceneImages()
    {
        // Sahnedeki (pasif olanlar dahil) tüm Image bileþenlerini bulur
        Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();
        int count = 0;

        foreach (Image img in allImages)
        {
            // Sadece aktif sahnede yer alan ve prefab varlýðý olmayan objelere uygula
            if (img.gameObject.scene.isLoaded)
            {
                Undo.RecordObject(img, "Change UI Material");
                img.material = targetMaterial;
                EditorUtility.SetDirty(img);
                count++;
            }
        }

        EditorUtility.DisplayDialog("Baþarýlý!", $"{count} adet Image bileþenine '{targetMaterial.name}' materyali baþarýyla atandý.", "Harika");
    }
}