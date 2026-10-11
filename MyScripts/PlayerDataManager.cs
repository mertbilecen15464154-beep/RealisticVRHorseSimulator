using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    [Serializable]
    public struct ShopCategory
    {
        public string categoryName;
        public GameObject categoryPanel;
    }

    [Serializable]
    private class PurchasedHorsesData
    {
        public List<string> purchasedHorseIDs = new();
    }

    [Serializable]
    public struct MapData
    {
        public string mapNameKey;
        public Image mapImage;
        public string sceneToLoad;
    }

    [Header("XR Keyboard Reference")]
    [SerializeField] private GameObject xrSpatialKeyboard;

    [Header("VR Input Settings")]
    [SerializeField] private InputActionProperty toggleMenuAction;

    [Header("Main Menu Master Objects")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject miniMapObject;

    [Header("Main Navigation Panels")]
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject horseMarketPanel;
    [SerializeField] private GameObject shopMasterPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Navigation Buttons")]
    [SerializeField] private Button playMenuButton;
    [SerializeField] private Button profileMenuButton;
    [SerializeField] private Button mapMenuButton;
    [SerializeField] private Button horseMarketMenuButton;
    [SerializeField] private Button shopMenuButton;
    [SerializeField] private Button settingsMenuButton;
    [SerializeField] private Button returnToFarmMenuButton;

    [Header("Shop Sub-Categories")]
    [SerializeField] private List<ShopCategory> shopCategories = new();

    [Header("Play & Race Navigation")]
    [SerializeField] private GameObject playPanel;
    [SerializeField] private Image mapDisplayImage;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text raceModeText;
    [SerializeField] private TMP_Text lapCountText;
    [SerializeField] private LocalizedUIElement localizedUI;

    [Header("Play Panel Buttons")]
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button previousMapButton;
    [SerializeField] private Button nextRaceModeButton;
    [SerializeField] private Button previousRaceModeButton;
    [SerializeField] private Button increaseLapsButton;
    [SerializeField] private Button decreaseLapsButton;
    [SerializeField] private Button startRaceButton;

    [Header("Map Configurations")]
    [SerializeField] private List<MapData> availableMaps = new();

    [Header("Economy & Profile UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemText;

    [Header("Settings UI References")]
    [SerializeField] private TMP_Text languageText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Settings Panel Buttons")]
    [SerializeField] private Button nextLanguageButton;
    [SerializeField] private Button previousLanguageButton;
    [SerializeField] private Button nextSubtitleButton;
    [SerializeField] private Button previousSubtitleButton;

    [Header("Confirmation Modal & Buttons")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button openConfirmationButton;
    [SerializeField] private Button confirmResetButton;
    [SerializeField] private Button cancelResetButton;

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip goldPurchaseSound;
    [SerializeField] private AudioClip gemPurchaseSound;
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioClip menuOpenSound;
    [SerializeField] private AudioClip menuCloseSound;

    [Header("Horse Spawning System")]
    [Tooltip("Çiftlikte aktif atýn duracaðý doðma noktasý")]
    [SerializeField] private Transform farmHorseSpawnPoint;

    // PlayerPrefs Keys
    public const string KEY_NAME = "PlayerData_Name";
    public const string KEY_LEVEL = "PlayerData_Level";
    public const string KEY_RANK = "PlayerData_Rank";
    public const string KEY_GOLD = "PlayerData_Gold";
    public const string KEY_GEMS = "PlayerData_Gems";
    public const string KEY_PURCHASED_HORSES = "PlayerData_PurchasedHorses";
    public const string KEY_EQUIPPED_HORSE = "PlayerData_EquippedHorse";
    private const string KEY_MINIMAP = "Settings_MiniMapActive";

    // Dynamic Variables
    public int CurrentGold { get; private set; }
    public int CurrentGems { get; private set; }
    private PurchasedHorsesData purchasedData = new();
    private string activeEquippedHorseID = "";
    private GameObject currentSpawnedHorseInstance;

    // Seçim Hafýza Deðiþkenleri
    private int currentMapIndex = 0;
    private RaceModeType selectedRaceMode = RaceModeType.ClassicRace;
    private int selectedLaps = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (toggleMenuAction.action != null)
        {
            toggleMenuAction.action.Enable();
            toggleMenuAction.action.performed += OnMenuButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (toggleMenuAction.action != null)
        {
            toggleMenuAction.action.performed -= OnMenuButtonPressed;
            toggleMenuAction.action.Disable();
        }
    }

    private void Start()
    {
        SetupButtonListeners();
        HideAllPanels();
        LoadPlayerData();

        // Baþlangýçta menü panelini tamamen kapatýyoruz
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Buton týklama olaylarýný kod üzerinden baðlar. 
    /// Inspector'dan On Click () ayarlama zorunluluðunu ortadan kaldýrýr.
    /// </summary>
    private void SetupButtonListeners()
    {
        // Ana Menü Navigasyon Butonlarý
        if (playMenuButton != null) playMenuButton.onClick.AddListener(OpenPlayPanel);
        if (profileMenuButton != null) profileMenuButton.onClick.AddListener(OpenProfilePanel);
        if (mapMenuButton != null) mapMenuButton.onClick.AddListener(OpenMapPanel);
        if (horseMarketMenuButton != null) horseMarketMenuButton.onClick.AddListener(OpenHorseMarketPanel);
        if (shopMenuButton != null) shopMenuButton.onClick.AddListener(OpenShopPanel);
        if (settingsMenuButton != null) settingsMenuButton.onClick.AddListener(OpenSettingsPanel);
        if (returnToFarmMenuButton != null)
        {
            returnToFarmMenuButton.onClick.AddListener(() => {
                if (RaceManager.Instance != null)
                {
                    RaceManager.Instance.ReturnToFarmOpenWorld();
                }
                else
                {
                    // Eðer haritada RaceManager yoksa varsayýlan sahneyi yükle
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Farm");
                }
            });
        }

        // Play Paneli Butonlarý
        if (nextMapButton != null) nextMapButton.onClick.AddListener(NextMap);
        if (previousMapButton != null) previousMapButton.onClick.AddListener(PreviousMap);
        if (nextRaceModeButton != null) nextRaceModeButton.onClick.AddListener(NextRaceMode);
        if (previousRaceModeButton != null) previousRaceModeButton.onClick.AddListener(PreviousRaceMode);
        if (increaseLapsButton != null) increaseLapsButton.onClick.AddListener(IncreaseLaps);
        if (decreaseLapsButton != null) decreaseLapsButton.onClick.AddListener(DecreaseLaps);
        if (startRaceButton != null) startRaceButton.onClick.AddListener(StartSelectedRace);

        // Ayarlar Paneli Butonlarý
        if (nextLanguageButton != null) nextLanguageButton.onClick.AddListener(OnNextLanguagePressed);
        if (previousLanguageButton != null) previousLanguageButton.onClick.AddListener(OnPreviousLanguagePressed);
        if (nextSubtitleButton != null) nextSubtitleButton.onClick.AddListener(OnNextSubtitlePressed);
        if (previousSubtitleButton != null) previousSubtitleButton.onClick.AddListener(OnPreviousSubtitlePressed);

        // Sýfýrlama Onay Paneli Butonlarý
        if (openConfirmationButton != null) openConfirmationButton.onClick.AddListener(OpenConfirmationPanel);
        if (confirmResetButton != null) confirmResetButton.onClick.AddListener(ConfirmResetPlayerData);
        if (cancelResetButton != null) cancelResetButton.onClick.AddListener(CloseConfirmationPanel);
    }

    // --- PLAY PANEL FONKSÝYONLARI ---

    public void OpenPlayPanel()
    {
        HideAllPanels();
        if (playPanel != null) playPanel.SetActive(true);
        UpdatePlayUI();
    }

    public void NextMap()
    {
        if (availableMaps.Count == 0) return;
        currentMapIndex = (currentMapIndex + 1) % availableMaps.Count;
        UpdatePlayUI();
    }

    public void PreviousMap()
    {
        if (availableMaps.Count == 0) return;
        currentMapIndex--;
        if (currentMapIndex < 0) currentMapIndex = availableMaps.Count - 1;
        UpdatePlayUI();
    }

    public void NextRaceMode()
    {
        selectedRaceMode = selectedRaceMode == RaceModeType.ClassicRace ? RaceModeType.CombatRace : RaceModeType.ClassicRace;
        UpdatePlayUI();
    }

    public void PreviousRaceMode()
    {
        NextRaceMode();
    }

    public void IncreaseLaps()
    {
        if (selectedLaps < 3)
        {
            selectedLaps++;
            UpdatePlayUI();
        }
    }

    public void DecreaseLaps()
    {
        if (selectedLaps > 1)
        {
            selectedLaps--;
            UpdatePlayUI();
        }
    }

    public void StartSelectedRace()
    {
        if (availableMaps.Count == 0) return;

        PlayerPrefs.SetInt("SelectedRaceMode", (int)selectedRaceMode);
        PlayerPrefs.SetInt("SelectedLaps", selectedLaps);
        PlayerPrefs.Save();

        string sceneName = availableMaps[currentMapIndex].sceneToLoad;
        if (!string.IsNullOrEmpty(sceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void UpdatePlayUI()
    {
        if (availableMaps.Count > 0 && currentMapIndex < availableMaps.Count)
        {
            var map = availableMaps[currentMapIndex];

            foreach (var m in availableMaps)
            {
                if (m.mapImage != null) m.mapImage.gameObject.SetActive(false);
            }

            if (map.mapImage != null)
            {
                map.mapImage.gameObject.SetActive(true);
            }

            if (mapNameText != null) mapNameText.text = map.mapNameKey;
        }

        if (raceModeText != null)
        {
            raceModeText.text = selectedRaceMode == RaceModeType.ClassicRace ? "Classic" : "Combat";
        }

        if (lapCountText != null)
        {
            lapCountText.text = selectedLaps.ToString();
        }

        if (localizedUI != null)
        {
            localizedUI.UpdateAllTexts();
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        ToggleMainUI();
    }

    #region Menu Toggle & Panel Switching Controls

    public void ToggleMainUI()
    {
        // Eðer yarýþ bitmiþse VR menüsünün açýlmasýný engelle
        if (RaceManager.Instance != null && RaceManager.Instance.isRaceFinished)
        {
            return;
        }

        if (menuPanel == null) return;

        bool isCurrentlyActive = menuPanel.activeSelf;
        bool nextState = !isCurrentlyActive;

        if (nextState)
        {
            HideAllPanels();
            OpenPlayPanel();
            menuPanel.SetActive(true);
        }
        else
        {
            HideAllPanels();
            menuPanel.SetActive(false);
        }

        if (audioSource != null)
        {
            AudioClip clipToPlay = nextState ? menuOpenSound : menuCloseSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
    }

    private void HideAllPanels()
    {
        if (playPanel != null) playPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (horseMarketPanel != null) horseMarketPanel.SetActive(false);
        if (shopMasterPanel != null) shopMasterPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (xrSpatialKeyboard != null) xrSpatialKeyboard.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        HideAllShopSubPanels();
    }

    private void HideAllShopSubPanels()
    {
        foreach (var category in shopCategories)
        {
            if (category.categoryPanel != null)
            {
                category.categoryPanel.SetActive(false);
            }
        }
    }

    public void OpenProfilePanel()
    {
        HideAllPanels();
        if (profilePanel != null) profilePanel.SetActive(true);
    }

    public void OpenMapPanel()
    {
        HideAllPanels();
        if (mapPanel != null) mapPanel.SetActive(true);
    }

    public void OpenHorseMarketPanel()
    {
        HideAllPanels();
        if (horseMarketPanel != null) horseMarketPanel.SetActive(true);
    }

    public void OpenShopPanel()
    {
        HideAllPanels();
        if (shopMasterPanel != null) shopMasterPanel.SetActive(true);

        if (shopCategories.Count > 0)
        {
            OpenShopCategoryByName(shopCategories[0].categoryName);
        }
    }

    public void OpenShopCategoryByName(string categoryName)
    {
        if (shopMasterPanel != null && !shopMasterPanel.activeSelf)
        {
            HideAllPanels();
            shopMasterPanel.SetActive(true);
        }

        HideAllShopSubPanels();

        foreach (var category in shopCategories)
        {
            if (category.categoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            {
                if (category.categoryPanel != null)
                {
                    category.categoryPanel.SetActive(true);
                }
                break;
            }
        }
    }

    public void OpenSettingsPanel()
    {
        HideAllPanels();
        if (settingsPanel != null) settingsPanel.SetActive(true);
        UpdateSettingsUI();
    }

    public void UpdateSettingsUI()
    {
        if (LocalizationManager.Instance == null) return;

        if (languageText != null)
        {
            languageText.text = LocalizationManager.Instance.currentLanguage.ToString();
        }

        if (subtitleText != null)
        {
            subtitleText.text = LocalizationManager.Instance.currentSubtitleLanguage.ToString();
        }
    }

    public void OnNextLanguagePressed()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.NextLanguage();
            UpdateSettingsUI();
        }
    }

    public void OnPreviousLanguagePressed()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.PreviousLanguage();
            UpdateSettingsUI();
        }
    }

    public void OnNextSubtitlePressed()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.NextSubtitleLanguage();
            UpdateSettingsUI();
        }
    }

    public void OnPreviousSubtitlePressed()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.PreviousSubtitleLanguage();
            UpdateSettingsUI();
        }
    }

    #endregion

    #region Player Name, Economy & Data Persistence

    public void SavePlayerName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            string trimmedName = newName.Trim();
            PlayerPrefs.SetString(KEY_NAME, trimmedName);
            PlayerPrefs.Save();

            if (playerNameText != null)
                playerNameText.text = trimmedName;
        }
    }

    public void SavePlayerNameFromInputField(TMP_InputField inputField)
    {
        if (inputField != null) SavePlayerName(inputField.text);
    }

    public void LoadPlayerData()
    {
        string savedName = PlayerPrefs.GetString(KEY_NAME, "Player");
        int savedLevel = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        string savedRank = PlayerPrefs.GetString(KEY_RANK, "Newbie");

        CurrentGold = PlayerPrefs.GetInt(KEY_GOLD, 1000);
        CurrentGems = PlayerPrefs.GetInt(KEY_GEMS, 100);

        if (playerNameText != null) playerNameText.text = savedName;
        if (levelText != null) levelText.text = "Lvl " + savedLevel;
        if (rankText != null) rankText.text = savedRank;

        UpdateEconomyUI();

        bool isMiniMapActive = PlayerPrefs.GetInt(KEY_MINIMAP, 1) == 1;
        if (miniMapObject != null) miniMapObject.SetActive(isMiniMapActive);

        string json = PlayerPrefs.GetString(KEY_PURCHASED_HORSES, "");
        if (!string.IsNullOrEmpty(json))
        {
            purchasedData = JsonUtility.FromJson<PurchasedHorsesData>(json);
        }
        else
        {
            purchasedData = new PurchasedHorsesData();
        }

        activeEquippedHorseID = PlayerPrefs.GetString(KEY_EQUIPPED_HORSE, "");

        InstantiateEquippedHorseOnStart();
    }

    public void UpdateEconomyUI()
    {
        if (goldText != null) goldText.text = CurrentGold.ToString();
        if (gemText != null) gemText.text = CurrentGems.ToString();
    }

    private void SaveEconomy()
    {
        PlayerPrefs.SetInt(KEY_GOLD, CurrentGold);
        PlayerPrefs.SetInt(KEY_GEMS, CurrentGems);
        PlayerPrefs.Save();
        UpdateEconomyUI();
    }

    private void SavePurchasedHorses()
    {
        string json = JsonUtility.ToJson(purchasedData);
        PlayerPrefs.SetString(KEY_PURCHASED_HORSES, json);
        PlayerPrefs.Save();
    }

    public bool IsHorsePurchased(string horseID)
    {
        return purchasedData.purchasedHorseIDs.Contains(horseID);
    }

    public bool IsHorseEquipped(string horseID)
    {
        if (string.IsNullOrEmpty(horseID) || string.IsNullOrEmpty(activeEquippedHorseID))
            return false;

        return activeEquippedHorseID == horseID;
    }

    #endregion

    #region Purchasing & Equipping Logic

    public void TryBuyHorse(HorseItem horseItem, bool isGem)
    {
        if (horseItem == null || IsHorsePurchased(horseItem.horseID)) return;

        if (isGem)
        {
            if (CurrentGems >= horseItem.gemPrice)
            {
                CurrentGems -= horseItem.gemPrice;
                CompleteHorsePurchase(horseItem, isGem);
            }
        }
        else
        {
            if (CurrentGold >= horseItem.goldPrice)
            {
                CurrentGold -= horseItem.goldPrice;
                CompleteHorsePurchase(horseItem, isGem);
            }
        }
    }

    private void CompleteHorsePurchase(HorseItem horseItem, bool isGem)
    {
        SaveEconomy();

        purchasedData.purchasedHorseIDs.Add(horseItem.horseID);
        SavePurchasedHorses();

        if (audioSource != null)
        {
            AudioClip clipToPlay = isGem ? gemPurchaseSound : goldPurchaseSound;
            if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
        }

        RefreshAllHorseItemsUI();
    }

    public void EquipHorse(HorseItem horseItem)
    {
        if (horseItem == null || !IsHorsePurchased(horseItem.horseID)) return;

        activeEquippedHorseID = horseItem.horseID;
        PlayerPrefs.SetString(KEY_EQUIPPED_HORSE, activeEquippedHorseID);
        PlayerPrefs.Save();

        SpawnEquippedHorse(horseItem.horsePrefab);

        RefreshAllHorseItemsUI();

        if (audioSource != null && equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }
    }

    private void SpawnEquippedHorse(GameObject prefab)
    {
        if (currentSpawnedHorseInstance != null)
        {
            Destroy(currentSpawnedHorseInstance);
        }

        if (prefab != null && farmHorseSpawnPoint != null)
        {
            currentSpawnedHorseInstance = Instantiate(prefab, farmHorseSpawnPoint.position, farmHorseSpawnPoint.rotation);
        }
    }

    private void InstantiateEquippedHorseOnStart()
    {
        if (string.IsNullOrEmpty(activeEquippedHorseID)) return;

        HorseItem[] allHorseItems = FindObjectsByType<HorseItem>();
        foreach (var item in allHorseItems)
        {
            if (item.horseID == activeEquippedHorseID)
            {
                SpawnEquippedHorse(item.horsePrefab);
                break;
            }
        }
    }

    public void RefreshAllHorseItemsUI()
    {
        HorseItem[] allHorseItems = FindObjectsByType<HorseItem>();
        foreach (var item in allHorseItems)
        {
            item.CheckStatus();
        }
    }

    #endregion

    #region Mini Map Controls

    public void ToggleMiniMap(bool isOn)
    {
        if (miniMapObject != null)
        {
            miniMapObject.SetActive(isOn);
            PlayerPrefs.SetInt(KEY_MINIMAP, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region Confirmation Panel & Account Reset

    public void OpenConfirmationPanel()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(true);
    }

    public void CloseConfirmationPanel()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void ConfirmResetPlayerData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (currentSpawnedHorseInstance != null)
        {
            Destroy(currentSpawnedHorseInstance);
        }

        LoadPlayerData();
        RefreshAllHorseItemsUI();

        CloseConfirmationPanel();
        Debug.Log("[PlayerDataManager] Tüm hesap verileri sýfýrlandý.");
    }

    #endregion
}