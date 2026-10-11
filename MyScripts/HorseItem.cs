using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HorseItem : MonoBehaviour
{
    [Header("Horse Config")]
    public string horseID;              // EÞSÝZ kimlik (Örn: "horse_classic_01")
    public GameObject horsePrefab;     // Çiftlikte doðacak at prefab'i
    public int goldPrice = 400;
    public int gemPrice = 10;

    [Header("UI Component References")]
    [SerializeField] private Button buyGoldButton;
    [SerializeField] private Button buyGemButton;
    [SerializeField] private TMP_Text goldPriceText;
    [SerializeField] private TMP_Text gemPriceText;

    [Header("Owned & Use UI References")]
    [SerializeField] private TMP_Text ownedText;          // Owned / Sahip Olundu metni
    [SerializeField] private Button useButton;            // USE / KULLAN Butonu
    [SerializeField] private GameObject useButtonText;    // "KULLAN / USE" TMP_Text objesi
    [SerializeField] private GameObject inUseButtonText;  // "KULLANILDI / IN USE" TMP_Text objesi

    private void Awake()
    {
        if (buyGoldButton != null) buyGoldButton.onClick.AddListener(OnBuyWithGoldClicked);
        if (buyGemButton != null) buyGemButton.onClick.AddListener(OnBuyWithGemClicked);
        if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
    }

    private void Start()
    {
        if (goldPriceText != null) goldPriceText.text = goldPrice.ToString();
        if (gemPriceText != null) gemPriceText.text = gemPrice.ToString();

        CheckStatus();
    }

    private void OnEnable()
    {
        CheckStatus();
    }

    public void CheckStatus()
    {
        if (PlayerDataManager.Instance == null || string.IsNullOrEmpty(horseID)) return;

        bool isPurchased = PlayerDataManager.Instance.IsHorsePurchased(horseID);
        bool isEquipped = PlayerDataManager.Instance.IsHorseEquipped(horseID);

        if (isPurchased)
        {
            // 1. Satýn alýndýysa Gold ve Gem butonlarýný gizle
            if (buyGoldButton != null) buyGoldButton.gameObject.SetActive(false);
            if (buyGemButton != null) buyGemButton.gameObject.SetActive(false);

            // 2. Owned metnini göster
            if (ownedText != null) ownedText.gameObject.SetActive(true);

            // 3. USE butonunu aç ve kuþanma durumuna göre text objelerini deðiþtir
            if (useButton != null)
            {
                useButton.gameObject.SetActive(true);

                if (isEquipped)
                {
                    // Kuþanýlmýþsa: "IN USE" text objesini aç, "USE" text objesini kapat
                    if (inUseButtonText != null) inUseButtonText.SetActive(true);
                    if (useButtonText != null) useButtonText.SetActive(false);
                    useButton.interactable = false;
                }
                else
                {
                    // Kuþanýlmamýþsa: "USE" text objesini aç, "IN USE" text objesini kapat
                    if (inUseButtonText != null) inUseButtonText.SetActive(false);
                    if (useButtonText != null) useButtonText.SetActive(true);
                    useButton.interactable = true;
                }
            }
        }
        else
        {
            // --- SATIN ALINMADIYSA ---
            if (buyGoldButton != null) buyGoldButton.gameObject.SetActive(true);
            if (buyGemButton != null) buyGemButton.gameObject.SetActive(true);

            if (ownedText != null) ownedText.gameObject.SetActive(false);
            if (useButton != null) useButton.gameObject.SetActive(false);
        }
    }

    private void OnBuyWithGoldClicked()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.TryBuyHorse(this, isGem: false);
        }
    }

    private void OnBuyWithGemClicked()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.TryBuyHorse(this, isGem: true);
        }
    }

    private void OnUseClicked()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.EquipHorse(this);
        }
    }
}