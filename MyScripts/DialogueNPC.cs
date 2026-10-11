using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[System.Serializable]
public class ActionPair
{
    [Tooltip("Konuþma metni (Çoklu Dil Destekli)")]
    public LocalizedText responseText;

    [Tooltip("Animator'daki kutucuðun (State) BÝREBÝR ÝSMÝ")]
    public string animationStateName;

    [Tooltip("Bu animasyon hangi Animator Layer'ýnda duruyor?")]
    public string layerName;

    [Tooltip("Oynatýlacak ses klibi (Çoklu Dil Destekli)")]
    public LocalizedAudio audioClip;

    public string GetText()
    {
        if (responseText == null) return "";
        GameLanguage activeLang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : GameLanguage.Turkish;
        return responseText.GetText(activeLang);
    }

    public AudioClip GetAudio()
    {
        if (audioClip == null) return null;
        GameLanguage activeLang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : GameLanguage.Turkish;
        return audioClip.GetAudio(activeLang);
    }
}

[System.Serializable]
public class DialogueOption
{
    [Tooltip("Oyuncunun verebileceði yanýt varyasyonlarý (Sýrasýyla A1, A2, A3...)")]
    public List<ActionPair> playerResponses = new();

    [Tooltip("NPC'nin verebileceði cevap varyasyonlarý (Sýrasýyla A1, A2, A3...)")]
    public List<ActionPair> npcResponses = new();
}

[System.Serializable]
public class DialogueStage
{
    [Header("Aþama Tanýmý")]
    public string stageName = "Stage 1";

    [Header("Bu Aþamadaki Seçenekler")]
    public DialogueOption optionA;
    public DialogueOption optionB;
    public DialogueOption optionC;
}

public class DialogueNPC : MonoBehaviour
{
    [Header("NPC Identity")]
    [Tooltip("GameUI bildirimlerinde ve altyazýda görünecek NPC Ýsmi (Çoklu Dil Destekli)")]
    public LocalizedText npcName;

    [Header("Component References")]
    public AudioSource npcAudioSource;   // NPC'nin hoparlörü
    public AudioSource playerAudioSource; // Oyuncunun hoparlörü
    public Animator npcAnimator;

    [Header("UI References")]
    [Tooltip("Ýçinde butonlarýn bulunduðu Canvas/GameObject")]
    public GameObject dialogueUIPanel;

    [Tooltip("A, B ve C seçenekleri için UI Butonlarý")]
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;

    [Tooltip("Konuþmayý Kapatma/Çýkýþ Butonu")]
    public Button exitButton;

    [Tooltip("Çalmakta olan diyaloðu/sesi anýnda geçme butonu")]
    public Button skipButton;

    [Tooltip("Butonlarýn üzerindeki TextMeshPro bileþenleri")]
    public TextMeshProUGUI textA;
    public TextMeshProUGUI textB;
    public TextMeshProUGUI textC;

    [Header("Input Settings")]
    public InputActionProperty aButtonProperty;    // VR A Butonu / PC E
    public InputActionProperty gripButtonProperty;  // VR Grip Tuþu / PC E (Pazarlýk/El Tutma)
    public Transform playerTransform;               // Oyuncu
    public Transform playerRightHandTransform;      // Oyuncunun Sað Eli (VR Controller)
    public float interactionDistance = 3.0f;

    [Header("--- IDLE / AMBIENT ACTIONS ---")]
    public List<ActionPair> idleActions = new();
    public string defaultIdleStateName = "Idle";
    public string idleLayerName = "IDLE";
    public float minIdleInterval = 5f;
    public float maxIdleInterval = 10f;

    [Header("--- DIALOGUE STAGES (Çoklu Aþama Sistem) ---")]
    public List<DialogueStage> dialogueStages = new();
    private int currentStageIndex = 0;

    [Header("--- HANDSHAKE / BARGAIN ACTIONS ---")]
    [Tooltip("NPC'nin El Kemiði / Transform'u")]
    public Transform npcRightHandTransform;
    [Tooltip("NPC'nin Omuz/Gövde Transform'u")]
    public Transform npcShoulderTransform;

    public float handshakeDistance = 0.4f;
    public float maxArmExtension = 0.75f;
    public float shakeVelocityThreshold = 1.2f;

    [Tooltip("Pazarlýk aþamasýndaki Oyuncu Cümleleri (A1, A2, A3)")]
    public List<ActionPair> handshakePlayerResponses = new();
    [Tooltip("Pazarlýk aþamasýndaki NPC Cümleleri (A1, A2, A3)")]
    public List<ActionPair> handshakeNpcResponses = new();

    [Header("Object Swap References")]
    public GameObject showcaseObject; // Sadece model olan pasif obje
    public GameObject interactableObject; // Vitrindeki asýl obje

    private int currentHandshakeIndex = 0;
    private bool isHandshakeReady = false;
    private Vector3 previousHandPosition;

    private bool isInteracting = false;
    private bool isBusy = false;
    private bool skipRequested = false;
    private bool isFirstStage = true;
    private Coroutine idleCoroutine;

    private readonly Dictionary<List<ActionPair>, List<int>> idlePools = new();

    private void Start()
    {
        if (dialogueUIPanel != null) dialogueUIPanel.SetActive(false);

        if (textA == null && buttonA != null) textA = buttonA.GetComponentInChildren<TextMeshProUGUI>();
        if (textB == null && buttonB != null) textB = buttonB.GetComponentInChildren<TextMeshProUGUI>();
        if (textC == null && buttonC != null) textC = buttonC.GetComponentInChildren<TextMeshProUGUI>();

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(EndInteraction);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipDialogue);
            skipButton.gameObject.SetActive(false); // Baþlangýçta pasif
        }

        idleCoroutine = StartCoroutine(IdleRoutine());
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactionDistance)
        {
            if (!isInteracting)
            {
                if (GameUI.Instance != null)
                {
                    GameLanguage lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : GameLanguage.Turkish;
                    string talkMsg = lang switch
                    {
                        GameLanguage.English => "Press \"A\" or \"E\" to talk",
                        GameLanguage.German => "Drücke \"A\" oder \"E\" um zu sprechen",
                        _ => "Konuþmak için \"A\" veya \"E\" tuþuna bas"
                    };
                    GameUI.Instance.ShowText(talkMsg, this);
                }

                bool vrButtonPressed = aButtonProperty.action != null && aButtonProperty.action.WasPressedThisFrame();
                bool pcKeyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

                if (vrButtonPressed || pcKeyPressed)
                {
                    StartInteraction();
                }
            }
            else
            {
                HandleHandshakeLogic();
            }
        }
        else if (distance > interactionDistance)
        {
            if (GameUI.Instance != null)
            {
                GameUI.Instance.HideText(this);
            }

            if (isInteracting)
            {
                EndInteraction();
            }
        }
    }

    public void SkipDialogue()
    {
        if (isBusy)
        {
            skipRequested = true;
        }
    }

    #region 1. IDLE / AMBIENT
    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            // YENÝ KONTROL: Hem meþgul deðilse HEM DE (etkileþimde deðilse VEYA henüz ilk diyalog seçimi yapýlmadýysa)
            if (!isBusy && (!isInteracting || isFirstStage) && idleActions.Count > 0)
            {
                float actionDuration = PlayRandomIdleAction(idleActions, npcAudioSource, npcAnimator, idleLayerName);

                float timer = 0f;
                while (timer < actionDuration)
                {
                    // Seçim yapýldýðý an (isFirstStage false olduðunda) idle aksiyonunu kesmek için:
                    if (isBusy || (isInteracting && !isFirstStage)) break;
                    timer += Time.deltaTime;
                    yield return null;
                }

                if (!isBusy && (!isInteracting || isFirstStage) && npcAnimator != null)
                {
                    int layerIndex = npcAnimator.GetLayerIndex(idleLayerName);
                    npcAnimator.CrossFade(defaultIdleStateName, 0.25f, layerIndex < 0 ? 0 : layerIndex);
                }

                float restTime = Random.Range(minIdleInterval, maxIdleInterval);
                timer = 0f;
                while (timer < restTime)
                {
                    if (isBusy || (isInteracting && !isFirstStage)) break;
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }
    }
    #endregion

    #region 2. INTERACTION & DIALOGUE UI
    public void StartInteraction()
    {
        isInteracting = true;
        isBusy = false;
        isFirstStage = true; // Ýlk aþamadayýz
        currentStageIndex = 0;
        currentHandshakeIndex = 0;
        isHandshakeReady = false;

        if (GameUI.Instance != null)
        {
            GameUI.Instance.HideText(this);
        }

        idleCoroutine ??= StartCoroutine(IdleRoutine());

        UpdateDialogueUIForCurrentStage();
    }

    private void UpdateDialogueUIForCurrentStage()
    {
        if (dialogueStages == null || dialogueStages.Count == 0) return;

        DialogueStage currentStage = dialogueStages[0];

        if (textA != null && currentStageIndex < currentStage.optionA.playerResponses.Count)
            textA.text = currentStage.optionA.playerResponses[currentStageIndex].GetText();

        if (textB != null && currentStageIndex < currentStage.optionB.playerResponses.Count)
            textB.text = currentStage.optionB.playerResponses[currentStageIndex].GetText();

        if (textC != null && currentStageIndex < currentStage.optionC.playerResponses.Count)
            textC.text = currentStage.optionC.playerResponses[currentStageIndex].GetText();

        if (buttonA != null)
        {
            buttonA.onClick.RemoveAllListeners();
            buttonA.onClick.AddListener(() => OnOptionSelected(currentStage.optionA, "OPTION A"));
        }

        if (buttonB != null)
        {
            buttonB.onClick.RemoveAllListeners();
            buttonB.onClick.AddListener(() => OnOptionSelected(currentStage.optionB, "OPTION B"));
        }

        if (buttonC != null)
        {
            buttonC.onClick.RemoveAllListeners();
            buttonC.onClick.AddListener(() => OnOptionSelected(currentStage.optionC, "OPTION C"));
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(EndInteraction);
        }

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        if (dialogueUIPanel != null && !dialogueUIPanel.activeSelf)
            dialogueUIPanel.SetActive(true);
    }

    private void OnOptionSelected(DialogueOption selectedOption, string layerName)
    {
        if (!isBusy)
        {
            StartCoroutine(ProcessDialogueSequence(selectedOption, layerName));
        }
    }

    private IEnumerator ProcessDialogueSequence(DialogueOption selectedOption, string layerName)
    {
        isBusy = true;
        skipRequested = false;
        isFirstStage = false; // Oyuncu ilk cevabýný seçti! Artýk pürdikkat oyuncuya odaklanacak.

        if (dialogueUIPanel != null)
            dialogueUIPanel.SetActive(false);

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        if (npcAudioSource != null && npcAudioSource.isPlaying) npcAudioSource.Stop();
        if (playerAudioSource != null && playerAudioSource.isPlaying) playerAudioSource.Stop();

        if (npcAnimator != null)
        {
            int layerIndex = npcAnimator.GetLayerIndex(idleLayerName);
            npcAnimator.CrossFade(defaultIdleStateName, 0.15f, layerIndex < 0 ? 0 : layerIndex);
        }

        GameLanguage lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : GameLanguage.Turkish;
        string npcDisplayName = npcName != null ? npcName.GetText(lang) : "NPC";

        // 1. OYUNCU KONUÞUR
        if (selectedOption.playerResponses != null && currentStageIndex < selectedOption.playerResponses.Count)
        {
            ActionPair playerAction = selectedOption.playerResponses[currentStageIndex];

            if (GameUI.Instance != null)
                GameUI.Instance.ShowText($"<b>Oyuncu:</b> {playerAction.GetText()}", this);

            float playerDuration = PlaySequentialAction(playerAction, playerAudioSource, null, layerName);

            float timer = 0f;
            while (timer < playerDuration && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (playerAudioSource != null && playerAudioSource.isPlaying) playerAudioSource.Stop();
            skipRequested = false;
        }

        // 2. NPC CEVAP VERÝR
        if (selectedOption.npcResponses != null && currentStageIndex < selectedOption.npcResponses.Count)
        {
            ActionPair npcAction = selectedOption.npcResponses[currentStageIndex];

            if (GameUI.Instance != null)
                GameUI.Instance.ShowText($"<b>{npcDisplayName}:</b> {npcAction.GetText()}", this);

            float npcDuration = PlaySequentialAction(npcAction, npcAudioSource, npcAnimator, layerName);

            float timer = 0f;
            while (timer < npcDuration && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (npcAudioSource != null && npcAudioSource.isPlaying) npcAudioSource.Stop();
            skipRequested = false;
        }

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        if (GameUI.Instance != null)
            GameUI.Instance.HideText(this);

        if (npcAnimator != null)
        {
            int layerIndex = npcAnimator.GetLayerIndex(layerName);
            npcAnimator.CrossFade(defaultIdleStateName, 0.25f, layerIndex < 0 ? 0 : layerIndex);
        }

        currentStageIndex++;

        if (isInteracting && selectedOption.playerResponses != null && currentStageIndex < selectedOption.playerResponses.Count)
        {
            isBusy = false;
            UpdateDialogueUIForCurrentStage();
        }
        else
        {
            if (handshakePlayerResponses != null && handshakePlayerResponses.Count > 0)
            {
                isHandshakeReady = true;
                isBusy = false;

                if (GameUI.Instance != null)
                {
                    string notificationMsg = lang switch
                    {
                        GameLanguage.English => $"Shake hands with [{npcDisplayName}] to bargain! (VR Grip / PC E)",
                        GameLanguage.German => $"Schützele [{npcDisplayName}] die Hand, um zu handeln!",
                        _ => $"Pazarlýk için [{npcDisplayName}] ile el sýkýþ! (VR Grip / PC E)"
                    };

                    GameUI.Instance.ShowText(notificationMsg, this);
                }
            }
            else
            {
                OnItemPurchased();
                EndInteraction();
            }
        }
    }
    #endregion

    #region 3. HANDSHAKE & BARGAIN LOGIC
    private void HandleHandshakeLogic()
    {
        if (!isHandshakeReady) return;

        bool vrGripPressed = gripButtonProperty.action != null && gripButtonProperty.action.IsPressed();
        bool pcEPressed = Keyboard.current != null && (Keyboard.current.eKey.isPressed || Keyboard.current.eKey.wasPressedThisFrame);

        if (pcEPressed && !isBusy)
        {
            if (GameUI.Instance != null)
            {
                GameUI.Instance.HideText(this);
            }

            StartCoroutine(ProcessBargainDialogueSequence());
            return;
        }

        if (playerRightHandTransform != null && npcRightHandTransform != null)
        {
            float distanceToHand = Vector3.Distance(playerRightHandTransform.position, npcRightHandTransform.position);

            if (distanceToHand <= handshakeDistance)
            {
                Vector3 targetHandPos = playerRightHandTransform.position;

                if (npcShoulderTransform != null)
                {
                    Vector3 shoulderToHand = targetHandPos - npcShoulderTransform.position;
                    if (shoulderToHand.magnitude > maxArmExtension)
                    {
                        targetHandPos = npcShoulderTransform.position + (shoulderToHand.normalized * maxArmExtension);
                    }
                }

                npcRightHandTransform.SetPositionAndRotation(
                    Vector3.Lerp(npcRightHandTransform.position, targetHandPos, Time.deltaTime * 12f),
                    Quaternion.Slerp(npcRightHandTransform.rotation, playerRightHandTransform.rotation, Time.deltaTime * 12f)
                );

                if (vrGripPressed)
                {
                    if (GameUI.Instance != null)
                    {
                        GameUI.Instance.HideText(this);
                    }

                    Vector3 handVelocity = (playerRightHandTransform.position - previousHandPosition) / Time.deltaTime;

                    if (handVelocity.magnitude > shakeVelocityThreshold && !isBusy)
                    {
                        StartCoroutine(ProcessBargainDialogueSequence());
                    }
                }

                previousHandPosition = playerRightHandTransform.position;
            }
        }
    }

    private IEnumerator ProcessBargainDialogueSequence()
    {
        if (currentHandshakeIndex >= handshakePlayerResponses.Count) yield break;

        isBusy = true;
        skipRequested = false;

        if (skipButton != null) skipButton.gameObject.SetActive(true);

        GameLanguage lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : GameLanguage.Turkish;
        string npcDisplayName = npcName != null ? npcName.GetText(lang) : "NPC";

        if (handshakePlayerResponses != null && currentHandshakeIndex < handshakePlayerResponses.Count)
        {
            ActionPair playerAction = handshakePlayerResponses[currentHandshakeIndex];

            if (GameUI.Instance != null)
                GameUI.Instance.ShowText($"<b>Oyuncu:</b> {playerAction.GetText()}", this);

            float playerDuration = PlaySequentialAction(playerAction, playerAudioSource, null, "HANDSHAKE");

            float timer = 0f;
            while (timer < playerDuration && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (playerAudioSource != null && playerAudioSource.isPlaying) playerAudioSource.Stop();
            skipRequested = false;
        }

        if (handshakeNpcResponses != null && currentHandshakeIndex < handshakeNpcResponses.Count)
        {
            ActionPair npcAction = handshakeNpcResponses[currentHandshakeIndex];

            if (GameUI.Instance != null)
                GameUI.Instance.ShowText($"<b>{npcDisplayName}:</b> {npcAction.GetText()}", this);

            float npcDuration = PlaySequentialAction(npcAction, npcAudioSource, npcAnimator, "HANDSHAKE");

            float timer = 0f;
            while (timer < npcDuration && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (npcAudioSource != null && npcAudioSource.isPlaying) npcAudioSource.Stop();
            skipRequested = false;
        }

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        if (GameUI.Instance != null)
            GameUI.Instance.HideText(this);

        currentHandshakeIndex++;

        if (currentHandshakeIndex >= handshakePlayerResponses.Count)
        {
            OnItemPurchased(); // Satýn almayý gerçekleþtir
            EndInteraction();  // Etkileþimi bitir ve IdleRoutine'i serbest býrak
        }
        else
        {
            isBusy = false; // Bir sonraki el sýkýþma hareketi için serbest býrak
        }
    }

    public void OnItemPurchased()
    {
        if (showcaseObject != null)
        {
            showcaseObject.SetActive(false);
        }

        if (interactableObject != null)
        {
            interactableObject.SetActive(true);
        }
    }

    public void EndInteraction()
    {
        isInteracting = false;
        isBusy = false; // Kilidi kaldýrarak IdleRoutine döngüsünün kaldýðý yerden devam etmesini saðlýyoruz
        isHandshakeReady = false;
        currentStageIndex = 0;
        currentHandshakeIndex = 0;

        if (dialogueUIPanel != null)
            dialogueUIPanel.SetActive(false);

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        if (GameUI.Instance != null)
        {
            GameUI.Instance.HideText(this);
        }

        idleCoroutine ??= StartCoroutine(IdleRoutine());
    }
    #endregion

    #region HELPER FUNCTIONS
    private float PlaySequentialAction(ActionPair action, AudioSource targetAudioSource, Animator targetAnimator, string defaultLayerName = "")
    {
        if (action == null) return 0f;

        float duration = 2.0f;

        AudioClip clipToPlay = action.GetAudio();

        if (clipToPlay != null && targetAudioSource != null)
        {
            targetAudioSource.Stop();
            targetAudioSource.clip = clipToPlay;
            targetAudioSource.Play();
            duration = clipToPlay.length;
        }

        if (!string.IsNullOrEmpty(action.animationStateName) && targetAnimator != null)
        {
            string targetLayer = !string.IsNullOrEmpty(action.layerName) ? action.layerName : defaultLayerName;
            int layerIndex = targetAnimator.GetLayerIndex(targetLayer);

            if (layerIndex >= 0)
            {
                targetAnimator.CrossFade(action.animationStateName, 0.2f, layerIndex);
            }
            else
            {
                targetAnimator.CrossFade(action.animationStateName, 0.2f, 0);
            }
        }

        return duration;
    }

    private float PlayRandomIdleAction(List<ActionPair> actionList, AudioSource targetAudioSource, Animator targetAnimator, string defaultLayerName = "")
    {
        if (actionList == null || actionList.Count == 0) return 0f;

        if (!idlePools.ContainsKey(actionList))
        {
            idlePools[actionList] = new List<int>();
        }

        List<int> pool = idlePools[actionList];

        if (pool.Count == 0)
        {
            for (int i = 0; i < actionList.Count; i++)
            {
                pool.Add(i);
            }
        }

        int randomPoolIndex = Random.Range(0, pool.Count);
        int selectedActionIndex = pool[randomPoolIndex];
        pool.RemoveAt(randomPoolIndex);

        return PlaySequentialAction(actionList[selectedActionIndex], targetAudioSource, targetAnimator, defaultLayerName);
    }
    #endregion
}