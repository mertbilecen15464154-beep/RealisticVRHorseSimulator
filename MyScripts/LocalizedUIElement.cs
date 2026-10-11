using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LocalizedUIElement : MonoBehaviour
{
    [System.Serializable]
    public struct LocalizedTextEntry
    {
        [Tooltip("Inspector'da kolay bulabilmek için açýklama ismi - Hierarchy'deki Obje Ýsmiyle Ayný Olmalý")]
        public string entryName;

        [Header("UI Reference")]
        [Tooltip("Deðiþtirilecek TextMeshPro bileþeni (Boþ býrakýrsanýz Hierarchy'den entryName ile otomatik aranýr)")]
        public TextMeshProUGUI textMesh;

        [Header("Language Texts")]
        public string turkishText;
        public string englishText;
        public string germanText;
    }

    [Header("Search Config")]
    public string searchFilter = "";
    public bool isCaseSensitive = false;

    [Header("Global UI Metin Listesi")]
    public List<LocalizedTextEntry> UIElements = new();

    // Editor animasyonu için Enter basýlma zamaný
    [HideInInspector] public double lastSearchTime = -100;

    private void Awake()
    {
        AutoFindReferencesInHierarchy();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;
        UpdateAllTexts();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    private void Start()
    {
        UpdateAllTexts();
    }

    /// <summary>
    /// TextMesh bileþeni atanmamýþ elemanlarý (Pasif/Disabled olanlar dahil) Hierarchy'deki entryName ismine göre otomatik bulur.
    /// </summary>
    private void AutoFindReferencesInHierarchy()
    {
        for (int i = 0; i < UIElements.Count; i++)
        {
            var entry = UIElements[i];

            // Eðer TextMesh zaten Inspector'dan elle atandýysa dokunma
            if (entry.textMesh != null) continue;

            // entryName boþsa es geç
            if (string.IsNullOrWhiteSpace(entry.entryName)) continue;

            // Pasif objeleri de kapsayan derin arama
            GameObject foundObj = FindGameObjectInScene(entry.entryName);

            if (foundObj != null)
            {
                // Objenin kendisinde veya pasif bileþenlerinde TextMeshProUGUI var mý bak
                if (foundObj.TryGetComponent<TextMeshProUGUI>(out var tmp))
                {
                    entry.textMesh = tmp;
                    UIElements[i] = entry; // Struct güncellemesi
                }
            }
        }
    }

    /// <summary>
    /// Aktif veya pasif fark etmeksizin sahnedeki tüm GameObjects içinde isme göre arama yapar.
    /// </summary>
    private GameObject FindGameObjectInScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            // Transform kütüphanesinin GetComponentsInChildren metodu True parametresiyle pasif objeleri de getirir
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t.name.Equals(objectName, StringComparison.Ordinal))
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }

    public void UpdateAllTexts()
    {
        GameLanguage lang = GameLanguage.Turkish;
        if (LocalizationManager.Instance != null)
        {
            lang = LocalizationManager.Instance.currentLanguage;
        }

        foreach (var entry in UIElements)
        {
            if (entry.textMesh == null) continue;

            string result = lang switch
            {
                GameLanguage.English => !string.IsNullOrEmpty(entry.englishText) ? entry.englishText : entry.turkishText,
                GameLanguage.German => !string.IsNullOrEmpty(entry.germanText) ? entry.germanText : entry.turkishText,
                _ => entry.turkishText
            };

            entry.textMesh.text = result;
        }
    }
}