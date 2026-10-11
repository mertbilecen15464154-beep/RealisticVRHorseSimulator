using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum RaceModeType
{
    ClassicRace,
    CombatRace
}

// Yarýþçý sonuçlarýný tutacak veri yapýsý
public class RaceResult
{
    public string racerName;
    public float finishTime;
    public bool isPlayer;
}

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Race Configuration")]
    [HideInInspector] public RaceModeType currentRaceMode = RaceModeType.ClassicRace;
    [HideInInspector] public int totalLaps = 1;
    [Tooltip("Altýnda Checkpoint objelerini sýrasýyla barýndýran Parent")]
    public Transform checkpointsParent;

    [Header("Race State")]
    public bool isRaceStarted = false;
    public bool isRaceFinished = false;
    public float raceTimer = 0f;
    public int currentLap = 1;

    [Header("Power-Up Settings")]
    public GameObject powerUpBoxesGroup;

    [Header("Target Horse Reference")]
    [Tooltip("Yarýþ bittiðinde yavaþlatýlacak oyuncu atý")]
    public GameObject playerHorsePrefab;
    public HorseControllerVR playerHorse;
    public float finishSlowdownDeceleration = 4f;

    [Header("UI References - In Game")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI raceHUDText;

    [Header("UI References - Race Finish Menu")]
    public GameObject raceFinishMenuPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI leaderboardText;
    public TextMeshProUGUI earnedGoldText;
    public TextMeshProUGUI earnedGemsText;
    public Button restartButton;
    public Button returnToFarmButton;
    public Button raceFinishSettingsButton;

    [Header("Economy Rewards")]
    public int rewardGoldFirstPlace = 500;
    public int rewardGoldCompletion = 150;
    public int rewardGemsFirstPlace = 10;

    [Header("Scene Settings")]
    public string farmSceneName = "OpenWorld_Farm";

    [Header("Referee & Audio Settings")]
    public Animator refereeAnimator;
    public string flagRaiseTriggerName = "RaiseFlag";
    public string flagLowerTriggerName = "LowerFlag";

    public AudioSource raceAudioSource;
    public AudioClip readyBeepClip;
    public AudioClip raceStartBellSound;
    public AudioClip raceFinishFanfare;

    [Header("Starting Gate Settings")]
    public Transform startingGateParent;
    public float gateMoveDelay = 3f;
    public Vector3 gateMoveOffset = new(0f, 0f, 10f);
    public float gateMoveSpeed = 3f;

    public List<Transform> leftGateDoors = new();
    public List<Transform> rightGateDoors = new();
    public float openAngle = 90f;
    public float doorOpenSpeed = 8f;

    [Header("AI Competitor Settings")]
    public GameObject aiHorsePrefab;
    [Tooltip("Altýnda AI spawn noktalarýný (Transform) barýndýran Parent")]
    public Transform aiSpawnParent;
    [Tooltip("Yarýþa katýlacak AI rakip sayýsý")]
    public int competitorCount = 3;

    [Header("AI Name Pool")]
    public List<string> randomHorseNames = new()
    {
        "Thunder", "Shadow", "Blaze", "Storm", "Maverick",
        "Apollo", "Phantom", "Titan", "Spirit", "Viper",
        "Zephyr", "Eclipse", "Pegasus", "Comet", "Diablo"
    };

    private readonly List<HorseAIController> spawnedAiHorses = new();
    private readonly List<Checkpoint> checkpointList = new();
    private int nextCheckpointIndex = 0;
    private Coroutine countdownCoroutine;
    private Coroutine gateMoveCoroutine;

    // Yarýþ Sonuçlarý
    private readonly List<RaceResult> raceResults = new();

    // Kapý Hafýza Yapýlarý
    private bool shouldOpenGates = false;
    private readonly List<Quaternion> leftInitialRotations = new();
    private readonly List<Quaternion> rightInitialRotations = new();
    private readonly List<Quaternion> leftTargetRotations = new();
    private readonly List<Quaternion> rightTargetRotations = new();

    private Vector3 initialGatePosition;
    private Vector3 targetGatePosition;
    private bool shouldMoveGateOffTrack = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Menüden kaydettiðimiz seçimleri yüklüyoruz
        currentRaceMode = (RaceModeType)PlayerPrefs.GetInt("SelectedRaceMode", (int)RaceModeType.ClassicRace);
        totalLaps = PlayerPrefs.GetInt("SelectedLaps", 1);
    }

    private void Start()
    {
        InitializeCheckpoints();
        SetupRaceMode();

        if (startingGateParent != null)
        {
            initialGatePosition = startingGateParent.position;
        }

        leftInitialRotations.Clear();
        leftTargetRotations.Clear();
        foreach (Transform door in leftGateDoors)
        {
            if (door != null)
            {
                Quaternion initRot = door.localRotation;
                leftInitialRotations.Add(initRot);
                leftTargetRotations.Add(initRot * Quaternion.Euler(0, 0, -openAngle));
            }
        }

        rightInitialRotations.Clear();
        rightTargetRotations.Clear();
        foreach (Transform door in rightGateDoors)
        {
            if (door != null)
            {
                Quaternion initRot = door.localRotation;
                rightInitialRotations.Add(initRot);
                rightTargetRotations.Add(initRot * Quaternion.Euler(0, 0, openAngle));
            }
        }

        SetupMenuButtonListeners();
    }

    private void SpawnCompetitors()
    {
        // 1. Eski AI atlarý güvenli bir þekilde temizle
        foreach (var ai in spawnedAiHorses)
        {
            if (ai != null && ai.gameObject != null)
            {
                if (ai.gameObject.scene.isLoaded)
                {
                    Destroy(ai.gameObject);
                }
            }
        }
        spawnedAiHorses.Clear();

        // 2. Sahne içi eski oyuncu atý varsa güvenli bir þekilde temizle
        if (playerHorse != null && playerHorse.gameObject != null)
        {
            if (playerHorse.gameObject.scene.isLoaded)
            {
                Destroy(playerHorse.gameObject);
            }
            playerHorse = null;
        }

        if (aiSpawnParent == null) return;

        int totalSpawnPoints = aiSpawnParent.childCount;
        if (totalSpawnPoints == 0) return;

        // Bütün Spawn Noktalarýný Hazýrla ve Karýþtýr (Fisher-Yates)
        List<Transform> availableSpawns = new();
        for (int i = 0; i < totalSpawnPoints; i++)
        {
            availableSpawns.Add(aiSpawnParent.GetChild(i));
        }

        for (int i = 0; i < availableSpawns.Count; i++)
        {
            Transform temp = availableSpawns[i];
            int randomIndex = UnityEngine.Random.Range(i, availableSpawns.Count);
            availableSpawns[i] = availableSpawns[randomIndex];
            availableSpawns[randomIndex] = temp;
        }

        int currentSpawnIndex = 0;

        // Oyuncu Atýný Prefab'tan Instantiate Et ve Rastgele Noktaya Koy
        if (playerHorsePrefab != null && currentSpawnIndex < availableSpawns.Count)
        {
            Transform playerSpawnPoint = availableSpawns[currentSpawnIndex];
            GameObject spawnedPlayerObj = Instantiate(playerHorsePrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);

            if (spawnedPlayerObj.TryGetComponent<HorseControllerVR>(out var controller))
            {
                playerHorse = controller;
            }
            currentSpawnIndex++;
        }

        // AI Atlarýný Kalan Benzersiz Spawn Noktalarýna Daðýt
        List<string> availableNames = new(randomHorseNames);
        int aiToSpawnCount = Mathf.Min(competitorCount, availableSpawns.Count - currentSpawnIndex);

        for (int i = 0; i < aiToSpawnCount; i++)
        {
            Transform aiSpawnPoint = availableSpawns[currentSpawnIndex];
            currentSpawnIndex++;

            GameObject newAiObj = Instantiate(aiHorsePrefab, aiSpawnPoint.position, aiSpawnPoint.rotation);

            string selectedName = "Rival";
            if (availableNames.Count > 0)
            {
                int nameIndex = UnityEngine.Random.Range(0, availableNames.Count);
                selectedName = availableNames[nameIndex];
                availableNames.RemoveAt(nameIndex);
            }

            newAiObj.name = selectedName;

            if (newAiObj.TryGetComponent<HorseAIController>(out var aiController))
            {
                spawnedAiHorses.Add(aiController);
            }
        }
    }

    public void OnAiFinished(HorseAIController ai)
    {
        if (ai != null)
        {
            ai.SetFinished();
        }

        if (ai != null && !raceResults.Exists(r => r.racerName == ai.gameObject.name))
        {
            raceResults.Add(new RaceResult
            {
                racerName = ai.gameObject.name,
                finishTime = raceTimer,
                isPlayer = false
            });

            UpdateLeaderboardUI();
        }
    }

    public void TriggerGateMove()
    {
        if (gateMoveCoroutine != null) StopCoroutine(gateMoveCoroutine);
        gateMoveCoroutine = StartCoroutine(MoveGateOffTrackRoutine());
    }

    private void SetupMenuButtonListeners()
    {
        if (restartButton != null) restartButton.onClick.AddListener(RestartRace); //
        if (returnToFarmButton != null) returnToFarmButton.onClick.AddListener(ReturnToFarmOpenWorld); //[cite: 17]

        if (raceFinishSettingsButton != null)
        {
            raceFinishSettingsButton.onClick.AddListener(() => {
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.OpenSettingsPanel();
                }
            });
        }
    }

    private void Update()
    {
        // Zaman sadece yarýþ baþladýysa VE yarýþ henüz bitmediyse aksýn
        if (isRaceStarted && !isRaceFinished)
        {
            raceTimer += Time.deltaTime;
            UpdateHUD();
        }

        if (shouldOpenGates)
        {
            for (int i = 0; i < leftGateDoors.Count; i++)
            {
                if (leftGateDoors[i] != null && i < leftTargetRotations.Count)
                {
                    leftGateDoors[i].localRotation = Quaternion.Slerp(leftGateDoors[i].localRotation, leftTargetRotations[i], Time.deltaTime * doorOpenSpeed);
                }
            }

            for (int i = 0; i < rightGateDoors.Count; i++)
            {
                if (rightGateDoors[i] != null && i < rightTargetRotations.Count)
                {
                    rightGateDoors[i].localRotation = Quaternion.Slerp(rightGateDoors[i].localRotation, rightTargetRotations[i], Time.deltaTime * doorOpenSpeed);
                }
            }
        }

        if (shouldMoveGateOffTrack && startingGateParent != null)
        {
            startingGateParent.position = Vector3.MoveTowards(startingGateParent.position, targetGatePosition, Time.deltaTime * gateMoveSpeed);

            if (Vector3.Distance(startingGateParent.position, targetGatePosition) < 0.01f)
            {
                shouldMoveGateOffTrack = false;
            }
        }
    }

    private void InitializeCheckpoints()
    {
        checkpointList.Clear();

        if (checkpointsParent != null)
        {
            Checkpoint[] foundCheckpoints = checkpointsParent.GetComponentsInChildren<Checkpoint>();

            for (int i = 0; i < foundCheckpoints.Length; i++)
            {
                foundCheckpoints[i].checkpointIndex = i;
                checkpointList.Add(foundCheckpoints[i]);
            }
        }
    }

    public void SetupRaceMode()
    {
        if (powerUpBoxesGroup != null)
        {
            powerUpBoxesGroup.SetActive(currentRaceMode == RaceModeType.CombatRace);
        }

        ResetRace();
    }

    public void ResetRace()
    {
        if (playerHorse != null)
        {
            playerHorse.isPlayerReady = false;
        }

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        if (gateMoveCoroutine != null) StopCoroutine(gateMoveCoroutine);

        isRaceStarted = false;
        isRaceFinished = false;
        raceTimer = 0f;
        currentLap = 1;
        nextCheckpointIndex = 0;
        raceResults.Clear();

        shouldOpenGates = false;
        shouldMoveGateOffTrack = false;

        if (startingGateParent != null)
        {
            startingGateParent.position = initialGatePosition;

            Collider[] gateColliders = startingGateParent.GetComponentsInChildren<Collider>();
            foreach (var col in gateColliders)
            {
                col.enabled = true;
            }
        }

        for (int i = 0; i < leftGateDoors.Count; i++)
        {
            if (leftGateDoors[i] != null && i < leftInitialRotations.Count)
            {
                leftGateDoors[i].localRotation = leftInitialRotations[i];
            }
        }

        for (int i = 0; i < rightGateDoors.Count; i++)
        {
            if (rightGateDoors[i] != null && i < rightInitialRotations.Count)
            {
                rightGateDoors[i].localRotation = rightInitialRotations[i];
            }
        }

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (raceHUDText != null) raceHUDText.gameObject.SetActive(false);
        if (raceFinishMenuPanel != null) raceFinishMenuPanel.SetActive(false);

        SpawnCompetitors();
    }

    public void StartRaceWithCountdown()
    {
        if (isRaceStarted || isRaceFinished) return;

        if (playerHorse == null || !playerHorse.isRiding)
        {
            Debug.LogWarning("Race cannot start: Player is not riding the horse!");
            return;
        }

        if (!playerHorse.isPlayerReady)
        {
            Debug.LogWarning("Race cannot start: Player is not READY!");
            return;
        }

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(StartRaceCountdownRoutine());
    }

    private IEnumerator StartRaceCountdownRoutine()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        if (countdownText != null) countdownText.text = "SET";
        PlayAudio(readyBeepClip, 0.6f);
        yield return new WaitForSeconds(1.2f);

        if (countdownText != null) countdownText.text = "READY";
        PlayAudio(readyBeepClip, 0.8f);

        if (refereeAnimator != null && !string.IsNullOrEmpty(flagRaiseTriggerName))
        {
            refereeAnimator.SetTrigger(flagRaiseTriggerName);
        }

        yield return new WaitForSeconds(1.2f);

        if (countdownText != null) countdownText.text = "GO!";

        shouldOpenGates = true;

        if (refereeAnimator != null && !string.IsNullOrEmpty(flagLowerTriggerName))
        {
            refereeAnimator.SetTrigger(flagLowerTriggerName);
        }

        PlayAudio(raceStartBellSound, 1f);

        isRaceStarted = true;
        isRaceFinished = false;

        if (raceHUDText != null)
        {
            raceHUDText.gameObject.SetActive(true);
            UpdateHUD();
        }

        yield return new WaitForSeconds(1.5f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    private IEnumerator MoveGateOffTrackRoutine()
    {
        yield return new WaitForSeconds(gateMoveDelay);

        if (startingGateParent != null)
        {
            Collider[] gateColliders = startingGateParent.GetComponentsInChildren<Collider>();
            foreach (var col in gateColliders)
            {
                col.enabled = false;
            }

            targetGatePosition = startingGateParent.position + gateMoveOffset;
            shouldMoveGateOffTrack = true;
        }
    }

    private void PlayAudio(AudioClip clip, float volume = 1f)
    {
        if (clip != null && raceAudioSource != null)
        {
            raceAudioSource.PlayOneShot(clip, volume);
        }
    }

    private void UpdateHUD()
    {
        if (raceHUDText != null)
        {
            raceHUDText.text = $"LAP: {currentLap}/{totalLaps}\nTIME: {FormatTime(raceTimer)}";
        }
    }

    public void OnCheckpointPassed(int checkpointIndex)
    {
        if (!isRaceStarted || isRaceFinished) return;

        if (checkpointIndex == nextCheckpointIndex)
        {
            nextCheckpointIndex++;

            if (nextCheckpointIndex >= checkpointList.Count)
            {
                nextCheckpointIndex = 0;

                if (currentLap >= totalLaps)
                {
                    FinishRace();
                }
                else
                {
                    currentLap++;
                }
            }
        }
    }

    private void FinishRace()
    {
        isRaceFinished = true;

        string currentPlayerDataName = PlayerPrefs.GetString(PlayerDataManager.KEY_NAME, "Player");

        // Oyuncu sonucunu ekle
        raceResults.Add(new RaceResult
        {
            racerName = currentPlayerDataName,
            finishTime = raceTimer,
            isPlayer = true
        });

        if (raceHUDText != null) raceHUDText.gameObject.SetActive(false);

        PlayAudio(raceFinishFanfare, 1f);

        // Hâlâ yarýþý bitirememiþ AI'lar varsa onlara tahmini/mevcut yarýþ süresinden biraz daha büyük süreler vererek tabloya ekle
        foreach (var ai in spawnedAiHorses)
        {
            if (ai != null && !raceResults.Exists(r => r.racerName == ai.gameObject.name))
            {
                // Henüz bitiremeyen AI'lar için rastgele küçük bir ek süre ile sýralamaya al
                float estimatedTime = raceTimer + UnityEngine.Random.Range(2.5f, 8.0f);
                raceResults.Add(new RaceResult
                {
                    racerName = ai.gameObject.name,
                    finishTime = estimatedTime,
                    isPlayer = false
                });
            }
        }

        StartCoroutine(SlowDownHorseRoutine());
    }

    private IEnumerator SlowDownHorseRoutine()
    {
        if (playerHorse != null)
        {
            playerHorse.GetType().GetField("whipCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerHorse, 0);
            playerHorse.GetType().GetField("speedTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerHorse, 0f);
            playerHorse.GetType().GetField("targetSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerHorse, 0f);

            float currentSpeed = (float)(playerHorse.GetType().GetField("currentSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(playerHorse) ?? 0f);

            while (currentSpeed > 0.05f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * finishSlowdownDeceleration);
                playerHorse.GetType().GetField("currentSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerHorse, currentSpeed);
                yield return null;
            }

            playerHorse.GetType().GetField("currentSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerHorse, 0f);
        }

        yield return new WaitForSeconds(0.5f);
        ShowFinishMenu();
    }

    private void ShowFinishMenu()
    {
        if (raceFinishMenuPanel == null) return;

        raceFinishMenuPanel.SetActive(true);

        int earnedGold = rewardGoldFirstPlace + rewardGoldCompletion;
        int earnedGems = rewardGemsFirstPlace;

        if (resultTitleText != null) resultTitleText.text = "RACE RESULTS";

        // finalTimeText satýrý tamamen kaldýrýldý

        if (earnedGoldText != null) earnedGoldText.text = $"+{earnedGold} GOLD";
        if (earnedGemsText != null) earnedGemsText.text = $"+{earnedGems} GEMS";

        UpdateLeaderboardUI();
    }

    private void UpdateLeaderboardUI()
    {
        if (leaderboardText == null) return;

        // Sürelere göre küçükten büyüðe sýrala
        raceResults.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));

        string leaderboardContent = "";
        for (int i = 0; i < raceResults.Count; i++)
        {
            var res = raceResults[i];
            string nameTag = res.isPlayer ? $"{res.racerName} (YOU)" : res.racerName;

            leaderboardContent += $"{i + 1}. {nameTag} - {FormatTime(res.finishTime)}\n";
        }

        leaderboardText.text = leaderboardContent;
    }

    public void RestartRace()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void ReturnToFarmOpenWorld()
    {
        if (!string.IsNullOrEmpty(farmSceneName))
        {
            SceneManager.LoadScene(farmSceneName);
        }
    }

    public string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);

        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
}