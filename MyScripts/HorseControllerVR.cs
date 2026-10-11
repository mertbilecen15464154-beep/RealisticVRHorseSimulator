using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class HorseControllerVR : MonoBehaviour
{
    [Header("VR Rider Settings")]
    public bool isRiding = false;
    public Transform saddleSeatPoint;
    public Transform exitPoint;
    public float interactionDistance = 3f;

    [Header("Hand Animator References")]
    public AnimateHandOnInput leftHandAnimator;
    public AnimateHandOnInput rightHandAnimator;

    [Header("Seat Settings")]
    [SerializeField] private Vector3 seatOffset = new(0f, 0.2f, 0f);

    [Header("XR Target Reference")]
    public XROrigin xrOrigin;

    [Header("Race System Link")]
    public RaceManager raceManager;

    [Header("Race Ready Controls")]
    public InputActionProperty leftTriggerAction;
    public InputActionProperty rightTriggerAction;
    public UnityEngine.UI.Image readyProgressBar;
    public GameObject readyUIContainer;
    public AudioSource readyAudioSource;
    public AudioClip readyChargingSound;
    public AudioClip readySuccessSound;

    public bool isPlayerReady = false;
    private float readyHoldTimer = 0f;
    private const float READY_HOLD_DURATION = 1.0f;

    [Header("Movement Speeds")]
    public float walkSpeed = 3f;
    public float trotSpeed = 6f;
    public float gallopSpeed = 12f;
    public float acceleration = 4f;
    public float backwardSpeed = 1.5f;
    public float rotateSpeed = 50f;
    public float brakingDeceleration = 8f;
    public float jumpForce = 8f;

    [Header("Grounded & Raycast Settings")]
    public LayerMask groundLayers = ~0; //
    public float groundCheckDistance = 0.8f; //
    public Transform groundCheckPoint; //
    public bool isGrounded = false; //[cite: 5]

    [Header("Custom Gravity Settings")]
    public float jumpGravity = -10f; // Yükselirken uygulanacak yer çekimi (Daha küçük eksi deðer = Daha hýzlý/akýcý yükselme)
    public float fallGravity = -30f; // Düþerken uygulanacak yer çekimi (Daha büyük eksi deðer = Hýzlý/tok düþüþ)
    private float verticalVelocity = 0f;

    [Header("Fall & Land Settings")]
    public float fallVelocityThreshold = -2.5f;

    [Header("Impact & Collision Settings")]
    public LayerMask obstacleLayers = ~0;
    public float minImpactSpeed = 2.5f;
    public float impactCooldown = 1.5f;

    [Header("Impact Knockback Settings")]
    public float knockbackMultiplier = 0.6f;
    public float maxKnockbackSpeed = 8f;

    [Header("VR Reins (XR Interaction Toolkit)")]
    public XRGrabInteractable leftManeRein;
    public XRGrabInteractable rightManeRein;
    public XRGrabInteractable leftSaddleRein;
    public XRGrabInteractable rightSaddleRein;
    public Transform reinsAnchorPoint;
    public float reinReturnSpeed = 8f;

    [Header("XR Controls")]
    public InputActionProperty mountHorseButton;
    public InputActionProperty jumpHorseButton;
    public InputActionProperty backKickButton;
    public InputActionProperty rearUpButton;
    public InputActionProperty maneControls;

    [Header("VR Components Control")]
    public GameObject locomotionObject;

    [Header("Realistic Control Thresholds")]
    public float reinWhipVelocityThreshold = 2.2f;
    public float speedDuration = 4f;
    public float stopPullDistance = 0.45f;
    public float turnSideDistance = 0.2f;

    [Header("Controller Position Inputs")]
    public InputActionProperty leftControllerPositionAction;  // XRI Left/Position
    public InputActionProperty rightControllerPositionAction; // XRI Right/Position

    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;

    [Header("VR Seat Stabilization")]
    public Transform saddleTargetBone;
    [Range(1, 100)] public int followSpeed = 10;
    [Range(1, 100)] public int rotationSmooth = 5;

    [Header("Petting System")]
    public Transform headPettingZone;
    public Collider[] allowedHandColliders;
    public AudioClip[] pettingSounds;
    public ParticleSystem pettingParticles;
    public float minHandVelocityForPet = 0.12f;
    public float pettingCooldown = 2.5f;

    private float lastPetTime = -99f;
    private Vector3 lastLeftHandPetPos;
    private Vector3 lastRightHandPetPos;

    [Header("Animation Settings")]
    public Animator horseAnimator;
    public float idleTransitionSpeed = 2f;

    [Header("Calling Horse Settings")]
    public float callingStoppingDistance = 2.5f;
    public float callingSpeed = 3f;
    public float rotationSpeed = 3f;
    public AudioSource callingAudioSource;
    public AudioClip[] callingAudioClips;

    private bool isCalled = false;

    [Header("Audio Settings - General")]
    public AudioSource horseAudioSource;
    public AudioSource footstepAudioSource;
    public AudioClip whipSound;
    public AudioClip jumpSound;
    public AudioClip slowDownVocalClip;
    public AudioClip[] rearUpSounds;
    public AudioClip[] knockbackSounds;
    public AudioClip[] backAttackSounds;
    public AudioClip[] footstepSounds;
    public AudioClip[] idleBreaths;

    [Header("Surface Footstep Sounds")]
    public float raycastDistance = 1.5f;
    public AudioClip[] dirtFootstepSounds;
    public AudioClip[] stoneFootstepSounds;
    public AudioClip[] grassFootstepSounds;
    public AudioClip[] woodFootstepSounds;
    public AudioClip[] defaultFootstepSounds;

    // Rigidbody Reference
    private Rigidbody rb;
    private CharacterController xrCharacterController;

    private float targetSpeed = 0f;
    private float currentSpeed = 0f;
    private float speedTimer = 0f;
    private int whipCount = 0;
    private float lastImpactTime = -99f;

    private float currentTurnAnim = 0f;
    private float currentSpeedAnim = 0f;
    private float targetIdleIndex = 0f;
    private float currentIdleIndexAnim = 0f;
    private float rawTurnInput = 0f;
    private bool isBraking = false;

    private Vector3 initialLeftManeReinPos;
    private Vector3 initialRightManeReinPos;
    private Vector3 initialLeftSaddleReinPos;
    private Vector3 initialRightSaddleReinPos;

    private Quaternion initialLeftManeReinRot;
    private Quaternion initialRightManeReinRot;
    private Quaternion initialLeftSaddleReinRot;
    private Quaternion initialRightSaddleReinRot;

    private float whipCooldownTimer = 0f;
    private const float WHIP_COOLDOWN = 0.5f;

    private bool hasPlayedSlowDownSound = false;
    private bool wasThumbstickUpPressed = false;

    private bool isWaitingInMainIdle = true;
    private bool hasPlayedIdleSound = false;
    private float lastFootstepTime = 0f;
    private const float FOOTSTEP_DEBOUNCE_TIME = 0.15f;

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");

    void Start()
    {
        // Eksik/boþ kalan referanslarý çalýþma anýnda otomatik baðlar
        AutoAssignMissingReferences();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Unity'nin kendi yer çekimini kapatýyoruz

        // 1. Atýn kendi kemik collider'larýnýn birbiriyle çakýþýp fýrlamasýný engelle
        Collider[] allBoneColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < allBoneColliders.Length; i++)
        {
            for (int j = i + 1; j < allBoneColliders.Length; j++)
            {
                Physics.IgnoreCollision(allBoneColliders[i], allBoneColliders[j], true);
            }
        }

        // 2. Binici (XR Origin) Collider'ý varsa atýn kemikleriyle çakýþmasýný engelle[cite: 5]
        if (xrOrigin == null) xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            xrCharacterController = xrOrigin.GetComponent<CharacterController>();
            Collider riderCollider = xrOrigin.GetComponentInChildren<Collider>();

            if (riderCollider != null)
            {
                foreach (var boneCollider in allBoneColliders)
                {
                    Physics.IgnoreCollision(boneCollider, riderCollider, true);
                }
            }
        }

        if (xrOrigin == null) xrOrigin = FindAnyObjectByType<XROrigin>();
        if (raceManager == null) raceManager = FindAnyObjectByType<RaceManager>();

        if (leftManeRein != null) { initialLeftManeReinPos = leftManeRein.transform.localPosition; initialLeftManeReinRot = leftManeRein.transform.localRotation; }
        if (rightManeRein != null) { initialRightManeReinPos = rightManeRein.transform.localPosition; initialRightManeReinRot = rightManeRein.transform.localRotation; }
        if (leftSaddleRein != null) { initialLeftSaddleReinPos = leftSaddleRein.transform.localPosition; initialLeftSaddleReinRot = leftSaddleRein.transform.localRotation; }
        if (rightSaddleRein != null) { initialRightSaddleReinPos = rightSaddleRein.transform.localPosition; initialRightSaddleReinRot = rightSaddleRein.transform.localRotation; }

        targetIdleIndex = 0f;
        currentIdleIndexAnim = 0f;

        if (horseAudioSource == null) horseAudioSource = gameObject.AddComponent<AudioSource>();
        if (footstepAudioSource == null) footstepAudioSource = gameObject.AddComponent<AudioSource>();

        horseAudioSource.spatialBlend = 1f;
        footstepAudioSource.spatialBlend = 1f;

        if (readyUIContainer != null) readyUIContainer.SetActive(false);
    }

    void Update()
    {
        CheckGround();
        HandleFallAndLandState();

        if (isRiding)
        {
            if (raceManager != null && !raceManager.isRaceStarted && !isPlayerReady)
            {
                bool leftTriggerPressed = leftTriggerAction.action != null && leftTriggerAction.action.ReadValue<float>() > 0.5f;
                bool rightTriggerPressed = rightTriggerAction.action != null && rightTriggerAction.action.ReadValue<float>() > 0.5f;

                if (leftTriggerPressed || rightTriggerPressed)
                {
                    if (readyUIContainer != null && !readyUIContainer.activeSelf) readyUIContainer.SetActive(true);

                    readyHoldTimer += Time.deltaTime;
                    float progress = Mathf.Clamp01(readyHoldTimer / READY_HOLD_DURATION);

                    if (readyProgressBar != null) readyProgressBar.fillAmount = progress;

                    if (readyAudioSource != null && readyChargingSound != null)
                    {
                        if (!readyAudioSource.isPlaying)
                        {
                            readyAudioSource.clip = readyChargingSound;
                            readyAudioSource.loop = true;
                            readyAudioSource.Play();
                        }
                        readyAudioSource.pitch = Mathf.Lerp(0.8f, 1.5f, progress);
                    }

                    if (readyHoldTimer >= READY_HOLD_DURATION)
                    {
                        isPlayerReady = true;
                        readyHoldTimer = 0f;

                        if (readyAudioSource != null)
                        {
                            readyAudioSource.Stop();
                            readyAudioSource.pitch = 1.0f;
                            if (readySuccessSound != null) readyAudioSource.PlayOneShot(readySuccessSound, 1f);
                        }

                        if (readyUIContainer != null) readyUIContainer.SetActive(false);
                        raceManager.StartRaceWithCountdown();
                    }
                }
                else
                {
                    ResetReadyState();
                }
            }
            else
            {
                if (readyUIContainer != null && readyUIContainer.activeSelf) readyUIContainer.SetActive(false);
            }

            ProcessRealisticReinControls();
            HandleJump();
            HandleBackAttackInput();
            HandleRearUpInput();
            HandleInsideInteraction();
        }
        else
        {
            PreventInteractionOutsideRiding();
            HandleOutsideInteraction();

            bool vrBButton = jumpHorseButton.action != null && jumpHorseButton.action.WasPressedThisFrame();
            if (vrBButton)
            {
                AudioSource sourceToUse = callingAudioSource != null ? callingAudioSource : horseAudioSource;
                if (sourceToUse != null && !sourceToUse.isPlaying)
                {
                    PlayRandomCallingSound();
                    isCalled = true;
                }
            }

            if (isCalled && xrOrigin != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, xrOrigin.transform.position);

                if (distanceToPlayer > callingStoppingDistance)
                {
                    Vector3 directionToPlayer = (xrOrigin.transform.position - transform.position).normalized;
                    directionToPlayer.y = 0f;

                    if (directionToPlayer != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                    }

                    currentSpeed = Mathf.MoveTowards(currentSpeed, callingSpeed, Time.deltaTime * 2f);
                }
                else
                {
                    isCalled = false;
                }
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 2.5f);
            }
        }

        UpdateAnimations();
        SmoothResetReins();
        UpdateIdleSystems();
    }

    private void FixedUpdate()
    {
        ApplyPhysicsMovement();
    }

    private Vector3 groundNormal = Vector3.up;

    private void CheckGround()
    {
        // GroundCheckPoint yoksa atýn merkezinden aþaðý doðru ölçer
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.up * 0.5f;

        // Rampalarda ve eðimlerde takýlmamasý için Raycast mesafesini net kontrol ediyoruz
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }

    private void ApplyPhysicsMovement()
    {
        if (rb == null) return;

        // 1. Kendi Yer Çekimi ve Zemin Sabitleme Mantýðýmýz
        if (isGrounded)
        {
            // Zýplama yükselme evresinde deðilsek (yere iniþ gerçekleþtiyse) dikey hýzý sýfýrla
            if (verticalVelocity <= 0f)
            {
                verticalVelocity = -2f; // Rampada ve zeminde tam tutunmasý için hafif bastýrma
            }
            else
            {
                // Yükselirken zemin algýlanýrsa (örneðin dik bir rampaya çarparsa) yükselmeye devam et
                verticalVelocity += jumpGravity * Time.fixedDeltaTime;
            }
        }
        else
        {
            // HAVADAYKEN: Yükselme ve Düþme evrelerine göre yer çekimi
            if (verticalVelocity > 0f)
            {
                verticalVelocity += jumpGravity * Time.fixedDeltaTime;
            }
            else
            {
                verticalVelocity += fallGravity * Time.fixedDeltaTime;
            }
        }

        // 2. Ýleri/geri yönü zemin eðimine göre hesapla
        Vector3 moveDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        Vector3 targetMoveVelocity = moveDirection * currentSpeed;

        // Dikey (Y) Hýzý hesaplanan deðerle güncelle
        targetMoveVelocity.y = verticalVelocity;

        // Yumuþak hareket geçiþi
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetMoveVelocity, Time.fixedDeltaTime * 10f);

        // 3. Dönüþ Mantýðý
        if (Mathf.Abs(currentSpeed) > 0.1f && Mathf.Abs(rawTurnInput) > 0.02f)
        {
            float directionMultiplier = currentSpeed > 0f ? 1f : -1f;
            float actualRotateSpeed = rotateSpeed * Mathf.Clamp01(Mathf.Abs(currentSpeed) / walkSpeed);

            Quaternion deltaRotation = Quaternion.Euler(0f, rawTurnInput * directionMultiplier * actualRotateSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    private void LateUpdate()
    {
        if (saddleSeatPoint != null && saddleTargetBone != null)
        {
            bool isJumping = !isGrounded;
            bool isSpecialAction = IsCustomAnimationActive() || isJumping;

            float activeFollowSpeed = isSpecialAction ? 100f : followSpeed;
            float activeRotationSmooth = isSpecialAction ? 3f : rotationSmooth;

            Vector3 targetPos = saddleTargetBone.TransformPoint(seatOffset);

            saddleSeatPoint.SetPositionAndRotation(
                Vector3.Lerp(saddleSeatPoint.position, targetPos, Time.deltaTime * activeFollowSpeed),
                Quaternion.Slerp(saddleSeatPoint.rotation, saddleTargetBone.rotation, Time.deltaTime * activeRotationSmooth)
            );
        }
    }

    private void ResetReadyState()
    {
        readyHoldTimer = 0f;
        if (readyProgressBar != null) readyProgressBar.fillAmount = 0f;
        if (readyUIContainer != null && readyUIContainer.activeSelf) readyUIContainer.SetActive(false);
        if (readyAudioSource != null && readyAudioSource.isPlaying)
        {
            readyAudioSource.Stop();
            readyAudioSource.pitch = 1.0f;
        }
    }

    private void HandleFallAndLandState()
    {
        if (horseAnimator == null) return;

        bool isFalling = !isGrounded && rb.linearVelocity.y < fallVelocityThreshold;

        horseAnimator.SetBool(IsGroundedHash, isGrounded);
        horseAnimator.SetBool(IsFallingHash, isFalling);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Çarptýðýmýz obje engel katmanýnda mý?
        if (((1 << collision.gameObject.layer) & obstacleLayers) == 0) return;

        // 2. Dikey zemin temaslarýný (ayaklarýn yere basmasýný) darbe sayma
        ContactPoint contact = collision.contacts[0];
        if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f) return;

        // 3. SADECE GALLOP (DÖRTNAL) HIZINDA ÇARPIÞMA ÝTME ETKÝSÝ OLSUN
        // trotSpeed ile gallopSpeed arasýndaki eþik deðerden hýzlýysa (Gallop durumundaysa)
        bool isGalloping = currentSpeed >= (trotSpeed + 0.5f);

        if (isGalloping && Time.time - lastImpactTime >= impactCooldown)
        {
            TriggerHorseImpact();
        }
        else
        {
            // Yürürken veya Týrýs giderken duvara çarparsa:
            // Sadece ileri hýz sýfýrlanýr, geri fýrlatýlma (knockback) gerçekleþmez.
            if (!isGalloping)
            {
                currentSpeed = 0f;
                targetSpeed = 0f;
                whipCount = 0;
                speedTimer = 0f;
            }
        }
    }

    private void TriggerHorseImpact()
    {
        lastImpactTime = Time.time;
        float calculatedKnockback = Mathf.Clamp(currentSpeed * knockbackMultiplier, 2f, maxKnockbackSpeed);

        currentSpeed = 0f;
        targetSpeed = 0f;
        whipCount = 0;
        speedTimer = 0f;

        if (horseAnimator != null) horseAnimator.SetTrigger("Knockback");

        if (knockbackSounds != null && knockbackSounds.Length > 0 && horseAudioSource != null)
        {
            AudioClip clip = knockbackSounds[UnityEngine.Random.Range(0, knockbackSounds.Length)];
            horseAudioSource.PlayOneShot(clip, 1f);
        }

        StartCoroutine(ApplyKnockbackRoutine(calculatedKnockback));
    }

    private IEnumerator ApplyKnockbackRoutine(float initialSpeed)
    {
        float knockbackTimer = 0f;
        float duration = 0.7f;
        Vector3 backwardDirection = -transform.forward;

        // 1. Geriye savrulma kuvvetini uygula
        while (knockbackTimer < duration)
        {
            float currentForce = Mathf.Lerp(initialSpeed, 0f, knockbackTimer / duration);
            Vector3 moveVector = backwardDirection * currentForce;
            moveVector.y = rb.linearVelocity.y;

            rb.linearVelocity = moveVector;

            knockbackTimer += Time.deltaTime;
            yield return null;
        }

        // 2. Knockback ve Recover animasyonlarý Animator üzerinde bitene kadar bekle
        while (IsCustomAnimationActive())
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            yield return null;
        }
    }

    private void HandleBackAttackInput()
    {
        if (raceManager != null && !raceManager.isRaceStarted && !isPlayerReady) return;

        var keyboard = Keyboard.current;
        bool keyboardBackAttack = keyboard != null && keyboard.fKey.wasPressedThisFrame;
        bool vrBackAttackPressed = backKickButton.action != null && backKickButton.action.WasPressedThisFrame();

        if ((keyboardBackAttack || vrBackAttackPressed) && Mathf.Abs(currentSpeed) < 0.1f && isGrounded)
        {
            if (horseAnimator != null) horseAnimator.SetTrigger("BackAttack");

            if (backAttackSounds != null && backAttackSounds.Length > 0 && horseAudioSource != null)
            {
                int randomIndex = UnityEngine.Random.Range(0, backAttackSounds.Length);
                if (backAttackSounds[randomIndex] != null) horseAudioSource.PlayOneShot(backAttackSounds[randomIndex], 1f);
            }
        }
    }

    private bool IsCustomAnimationActive()
    {
        if (horseAnimator == null) return false;

        AnimatorStateInfo stateInfo = horseAnimator.GetCurrentAnimatorStateInfo(0);

        bool isRearUp = stateInfo.IsName("Horse_RearUp") || stateInfo.IsTag("RearUp") ||
                        (horseAnimator.IsInTransition(0) && horseAnimator.GetNextAnimatorStateInfo(0).IsName("Horse_RearUp"));

        bool isBackAttack = stateInfo.IsName("BackAttack") || stateInfo.IsTag("BackAttack") ||
                            (horseAnimator.IsInTransition(0) && horseAnimator.GetNextAnimatorStateInfo(0).IsName("BackAttack"));

        bool isKnockback = stateInfo.IsName("Horse_Knockback") || stateInfo.IsTag("Knockback") ||
                           (horseAnimator.IsInTransition(0) && horseAnimator.GetNextAnimatorStateInfo(0).IsName("Horse_Knockback"));

        bool isRecover = stateInfo.IsName("Horse_Recover") || stateInfo.IsTag("Recover") ||
                         (horseAnimator.IsInTransition(0) && horseAnimator.GetNextAnimatorStateInfo(0).IsName("Horse_Recover"));

        return isRearUp || isBackAttack || isKnockback || isRecover;
    }

    private void HandleRearUpInput()
    {
        var keyboard = Keyboard.current;
        bool keyboardRearUp = keyboard != null && keyboard.rKey.wasPressedThisFrame;
        bool vrRearUpPressed = rearUpButton.action != null && rearUpButton.action.WasPressedThisFrame();

        if ((keyboardRearUp || vrRearUpPressed) && Mathf.Abs(currentSpeed) < 0.1f && isGrounded)
        {
            if (horseAnimator != null) horseAnimator.SetTrigger("RearUp");

            if (rearUpSounds != null && rearUpSounds.Length > 0 && horseAudioSource != null)
            {
                int randomIndex = UnityEngine.Random.Range(0, rearUpSounds.Length);
                if (rearUpSounds[randomIndex] != null) horseAudioSource.PlayOneShot(rearUpSounds[randomIndex], 1f);
            }
        }
    }

    private void ProcessRealisticReinControls()
    {
        if (whipCooldownTimer > 0f) whipCooldownTimer -= Time.deltaTime;

        bool vrWippedLeft = false;
        bool vrWippedRight = false;
        bool vrBrakingPull = false;

        float leftTurnAmount = 0f;
        float rightTurnAmount = 0f;

        // Kontrolcülerin anlýk pozisyonlarýný (XRI Left/Position ve XRI Right/Position) okuyoruz
        Vector3 currentLeftPos = leftControllerPositionAction.action != null ? leftControllerPositionAction.action.ReadValue<Vector3>() : Vector3.zero;
        Vector3 currentRightPos = rightControllerPositionAction.action != null ? rightControllerPositionAction.action.ReadValue<Vector3>() : Vector3.zero;

        // Pozisyon farkýndan (Delta Position / Time) elin o anki hýzýný türetiyoruz
        float deltaTime = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
        Vector3 leftHandVelocity = (currentLeftPos - lastLeftHandPos) / deltaTime;
        Vector3 rightHandVelocity = (currentRightPos - lastRightHandPos) / deltaTime;

        // Bir sonraki frame için pozisyonlarý saklýyoruz
        lastLeftHandPos = currentLeftPos;
        lastRightHandPos = currentRightPos;

        if (reinsAnchorPoint != null)
        {
            bool leftManeHeld = leftManeRein != null && leftManeRein.isSelected;
            bool rightManeHeld = rightManeRein != null && rightManeRein.isSelected;
            bool leftSaddleHeld = leftSaddleRein != null && leftSaddleRein.isSelected;
            bool rightSaddleHeld = rightSaddleRein != null && rightSaddleRein.isSelected;

            XRBaseInteractable activeLeft = leftManeHeld ? leftManeRein : (leftSaddleHeld ? leftSaddleRein : null);
            XRBaseInteractable activeRight = rightManeHeld ? rightManeRein : (rightSaddleHeld ? rightSaddleRein : null);

            bool leftHeld = activeLeft != null;
            bool rightHeld = activeRight != null;
            bool isUsingMane = leftManeHeld || rightManeHeld;

            Vector3 anchorHorseLocal = transform.InverseTransformPoint(reinsAnchorPoint.position);

            // 1. SOL EL KONTROLLERÝ
            if (leftHeld)
            {
                // KAMÇI: Pozisyondan hesaplanan aþaðý yönlü ivme reinWhipVelocityThreshold deðerini aþtý mý?
                float leftDownwardsVelocity = -leftHandVelocity.y;
                if (isGrounded && leftDownwardsVelocity > reinWhipVelocityThreshold && whipCooldownTimer <= 0f)
                {
                    vrWippedLeft = true;
                }

                // DÖNÜÞ: Sola yönelme
                Vector3 leftHorseLocal = transform.InverseTransformPoint(activeLeft.transform.position);
                float leftXOffset = leftHorseLocal.x - anchorHorseLocal.x;
                if (leftXOffset < -turnSideDistance) leftTurnAmount = (leftXOffset + turnSideDistance) * 2f;
            }

            // 2. SAÐ EL KONTROLLERÝ
            if (rightHeld)
            {
                // KAMÇI: Pozisyondan hesaplanan aþaðý yönlü ivme reinWhipVelocityThreshold deðerini aþtý mý?
                float rightDownwardsVelocity = -rightHandVelocity.y;
                if (isGrounded && rightDownwardsVelocity > reinWhipVelocityThreshold && whipCooldownTimer <= 0f)
                {
                    vrWippedRight = true;
                }

                // DÖNÜÞ: Saða yönelme
                Vector3 rightHorseLocal = transform.InverseTransformPoint(activeRight.transform.position);
                float rightXOffset = rightHorseLocal.x - anchorHorseLocal.x;
                if (rightXOffset > turnSideDistance) rightTurnAmount = (rightXOffset - turnSideDistance) * 2f;
            }

            // 3. GERÝ ÇEKME VEYA YELLESÝ (MANE) FREN/GERÝ GÝTME
            if (isUsingMane)
            {
                Vector2 thumbstickValue = Vector2.zero;
                if (maneControls.action != null) thumbstickValue = maneControls.action.ReadValue<Vector2>();

                if (thumbstickValue.y > 0.6f)
                {
                    if (!wasThumbstickUpPressed && whipCooldownTimer <= 0f)
                    {
                        vrWippedRight = true;
                        wasThumbstickUpPressed = true;
                    }
                }
                else wasThumbstickUpPressed = false;

                if (thumbstickValue.y < -0.6f) vrBrakingPull = true;
            }
            else
            {
                // GERÝ ÇEKME: Ýki el de tutulurken stopPullDistance mesafesi kadar geriye çekildi mi?
                Vector3 leftHorseLocal = leftHeld ? transform.InverseTransformPoint(activeLeft.transform.position) : Vector3.zero;
                Vector3 rightHorseLocal = rightHeld ? transform.InverseTransformPoint(activeRight.transform.position) : Vector3.zero;

                bool isPullingBackLeft = leftHeld && (anchorHorseLocal.z - leftHorseLocal.z) > stopPullDistance;
                bool isPullingBackRight = rightHeld && (anchorHorseLocal.z - rightHorseLocal.z) > stopPullDistance;

                bool isWhippingRightNowVR = vrWippedLeft || vrWippedRight;
                if ((isPullingBackLeft && isPullingBackRight) && !isWhippingRightNowVR)
                {
                    vrBrakingPull = true;
                }
            }
        }

        // Klavye Test Destekleri
        var keyboard = Keyboard.current;
        bool keyboardWhip = keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) && whipCooldownTimer <= 0f;
        bool keyboardBraking = keyboard != null && keyboard.sKey.isPressed;

        bool wipped = (vrWippedLeft || vrWippedRight || keyboardWhip) && isGrounded;
        if (raceManager != null && !raceManager.isRaceStarted && !isPlayerReady) wipped = false;

        bool isBrakingInput = vrBrakingPull || keyboardBraking;

        float keyboardTurnInput = 0f;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed) keyboardTurnInput = 1f;
            else if (keyboard.aKey.isPressed) keyboardTurnInput = -1f;
        }

        float vrTurnInput = leftTurnAmount + rightTurnAmount;
        rawTurnInput = Mathf.Abs(keyboardTurnInput) > 0.01f ? keyboardTurnInput : vrTurnInput;

        if (isBrakingInput)
        {
            if (!hasPlayedSlowDownSound && currentSpeed > 1f)
            {
                if (horseAudioSource != null && slowDownVocalClip != null) horseAudioSource.PlayOneShot(slowDownVocalClip);
                hasPlayedSlowDownSound = true;
            }

            bool canMoveBackward = !(raceManager != null && !raceManager.isRaceStarted && !isPlayerReady);

            whipCount = 0;
            speedTimer = 0f;
            targetSpeed = 0f;
            isBraking = canMoveBackward;

            if (currentSpeed > 0.05f) currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * brakingDeceleration);
            else if (canMoveBackward) currentSpeed = Mathf.MoveTowards(currentSpeed, -backwardSpeed, Time.deltaTime * 3f);
            else currentSpeed = 0f;
        }
        else
        {
            isBraking = false;
            hasPlayedSlowDownSound = false;

            if (wipped)
            {
                if (currentSpeed < 0f) currentSpeed = 0f;

                speedTimer = speedDuration;
                whipCount = Mathf.Clamp(whipCount + 1, 1, 3);

                if (whipCount == 1) targetSpeed = walkSpeed;
                else if (whipCount == 2) targetSpeed = trotSpeed;
                else if (whipCount == 3) targetSpeed = gallopSpeed;

                whipCooldownTimer = WHIP_COOLDOWN;
                PlayWhipSound();
            }

            if (whipCount > 0)
            {
                if (speedTimer > 0f)
                {
                    speedTimer -= Time.deltaTime;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * acceleration);
                }
                else
                {
                    whipCount--;
                    if (whipCount == 2) { targetSpeed = trotSpeed; speedTimer = speedDuration; }
                    else if (whipCount == 1) { targetSpeed = walkSpeed; speedTimer = speedDuration; }
                    else { targetSpeed = 0f; }
                }
            }
            else
            {
                if (currentSpeed > 0f) currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 2f);
                else if (currentSpeed < 0f) currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 4f);
            }
        }
    }

    private void HandleJump()
    {
        if (raceManager != null && !raceManager.isRaceStarted && !isPlayerReady) return;

        var keyboard = Keyboard.current;
        bool keyboardJump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        bool vrJumpPressed = jumpHorseButton.action != null && jumpHorseButton.action.WasPressedThisFrame();

        // Sadece kesin olarak zemindeyse VE halihazýrda yukarý doðru fýrlama hýzýnda deðilse zýpla
        if ((keyboardJump || vrJumpPressed) && isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = jumpForce;

            if (horseAnimator != null) horseAnimator.SetTrigger("Jump");
            if (jumpSound != null && horseAudioSource != null) horseAudioSource.PlayOneShot(jumpSound, 0.8f);
        }
    }

    private void UpdateAnimations()
    {
        if (horseAnimator == null) return;

        float targetTurnValue = Mathf.Clamp(rawTurnInput, -1f, 1f);
        currentTurnAnim = Mathf.Lerp(currentTurnAnim, targetTurnValue, Time.deltaTime * 5f);
        horseAnimator.SetFloat("Turn", currentTurnAnim);

        bool isBackingValue = currentSpeed < -0.05f || (isBraking && currentSpeed <= 0.05f);
        horseAnimator.SetBool("IsBacking", isBackingValue);

        float speedParamForAnimator = isBackingValue ? 0f : Mathf.Max(0f, currentSpeed);

        currentSpeedAnim = Mathf.Lerp(currentSpeedAnim, speedParamForAnimator, Time.deltaTime * 6f);
        horseAnimator.SetFloat("Speed", currentSpeedAnim);

        currentIdleIndexAnim = Mathf.MoveTowards(currentIdleIndexAnim, targetIdleIndex, Time.deltaTime * idleTransitionSpeed);
        horseAnimator.SetFloat("IdleIndex", currentIdleIndexAnim);
    }

    // HorseControllerVR.cs

    private void UpdateIdleSystems()
    {
        if (horseAnimator == null) return;

        // At hareket ediyorsa, zýplýyorsa veya özel bir aksiyon/saldýrý yapýyorsa IdleIndex sýfýrlanýr
        if (Mathf.Abs(currentSpeed) > 0.05f || IsCustomAnimationActive() || !isGrounded)
        {
            targetIdleIndex = 0f;
            isWaitingInMainIdle = true;
            hasPlayedIdleSound = false;
            return;
        }

        AnimatorStateInfo stateInfo = horseAnimator.GetCurrentAnimatorStateInfo(0);
        float animationProgress = stateInfo.normalizedTime % 1f;

        // Ana Idle tamamlandýðýnda rastgele yan Idle animasyonuna geç
        if (animationProgress >= 0.98f && !horseAnimator.IsInTransition(0))
        {
            if (isWaitingInMainIdle)
            {
                // 1 ile 3 arasýndaki yan idle animasyonlarýný tetikler
                targetIdleIndex = (float)UnityEngine.Random.Range(1, 4);
                isWaitingInMainIdle = false;

                if (!hasPlayedIdleSound)
                {
                    PlayRandomIdleSound();
                    hasPlayedIdleSound = true;
                }
            }
            else
            {
                // Yan Idle bitince tekrar ana Idle (0) durumuna döner
                targetIdleIndex = 0f;
                isWaitingInMainIdle = true;
                hasPlayedIdleSound = false;
            }
        }
        else if (animationProgress < 0.90f)
        {
            hasPlayedIdleSound = false;
        }
    }

    private void PlayWhipSound()
    {
        if (whipSound != null && horseAudioSource != null) horseAudioSource.PlayOneShot(whipSound, 0.9f);
    }

    private void PlayRandomIdleSound()
    {
        if (idleBreaths != null && idleBreaths.Length > 0 && horseAudioSource != null)
        {
            if (horseAudioSource.isPlaying) return;
            int randomIndex = UnityEngine.Random.Range(0, idleBreaths.Length);
            horseAudioSource.PlayOneShot(idleBreaths[randomIndex], 0.6f);
        }
    }

    public void PlayFootstepSoundEvent()
    {
        if (footstepAudioSource == null || !isGrounded) return;
        if (Time.time - lastFootstepTime < FOOTSTEP_DEBOUNCE_TIME) return;

        AudioClip[] targetSounds = defaultFootstepSounds;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, raycastDistance))
        {
            string surfaceTag = hit.collider.tag;

            targetSounds = surfaceTag switch
            {
                "Dirt" or "Toprak" => (dirtFootstepSounds != null && dirtFootstepSounds.Length > 0) ? dirtFootstepSounds : defaultFootstepSounds,
                "Stone" or "Tas" or "Taþ" => (stoneFootstepSounds != null && stoneFootstepSounds.Length > 0) ? stoneFootstepSounds : defaultFootstepSounds,
                "Grass" or "Cim" or "Çim" => (grassFootstepSounds != null && grassFootstepSounds.Length > 0) ? grassFootstepSounds : defaultFootstepSounds,
                "Wood" or "Ahsap" or "Ahþap" => (woodFootstepSounds != null && woodFootstepSounds.Length > 0) ? woodFootstepSounds : defaultFootstepSounds,
                _ => defaultFootstepSounds,
            };
        }

        if (targetSounds == null || targetSounds.Length == 0) return;

        AudioClip clipToPlay = targetSounds[UnityEngine.Random.Range(0, targetSounds.Length)];

        float volume = 0.4f;
        float pitchMin = 0.95f;
        float pitchMax = 1.05f;

        bool isTrotSpeed = currentSpeedAnim > walkSpeed + 0.5f && currentSpeedAnim <= trotSpeed + 0.5f;
        bool isTurningWhileTrot = currentSpeedAnim > walkSpeed - 0.5f && Mathf.Abs(currentTurnAnim) > 0.2f && currentSpeedAnim <= trotSpeed + 0.5f;

        if (isTrotSpeed || isTurningWhileTrot)
        {
            volume = 0.65f;
            pitchMin = 0.92f;
            pitchMax = 1.08f;
        }
        else if (currentSpeedAnim > trotSpeed + 0.5f)
        {
            volume = 0.9f;
            pitchMin = 0.88f;
            pitchMax = 1.04f;
        }

        footstepAudioSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        footstepAudioSource.PlayOneShot(clipToPlay, volume);

        lastFootstepTime = Time.time;
    }

    private void HandleOutsideInteraction()
    {
        var keyboard = Keyboard.current;
        bool keyboardMount = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame);
        bool vrMountPressed = mountHorseButton.action != null && mountHorseButton.action.WasPressedThisFrame();

        if (xrOrigin == null)
        {
            if (keyboardMount || vrMountPressed) MountHorse();
            return;
        }

        float dist = Vector3.Distance(xrOrigin.transform.position, transform.position);

        if (dist <= interactionDistance)
        {
            if (GameUI.Instance) GameUI.Instance.ShowText("Press 'A' to mount the horse", this);
            if (keyboardMount || vrMountPressed) MountHorse();
        }
        else
        {
            if (GameUI.Instance) GameUI.Instance.HideText(this);
        }
    }

    private void HandleInsideInteraction()
    {
        var keyboard = Keyboard.current;
        bool keyboardDismount = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame);
        bool vrDismountPressed = mountHorseButton.action != null && mountHorseButton.action.WasPressedThisFrame();

        if (keyboardDismount || vrDismountPressed) DismountHorse();
    }

    public void MountHorse()
    {
        isCalled = false;

        if (xrOrigin != null && saddleSeatPoint != null)
        {
            // XR Origin üzerindeki CharacterController pasife alýnýyor (Atýn hareketini etkilemesin diye)
            if (xrCharacterController != null) xrCharacterController.enabled = false;
            if (locomotionObject != null) locomotionObject.SetActive(false);

            xrOrigin.transform.SetParent(saddleSeatPoint);

            if (xrOrigin.Camera != null)
            {
                Vector3 cameraLocalPos = xrOrigin.Camera.transform.localPosition;
                xrOrigin.transform.localPosition = -cameraLocalPos;
            }
            else xrOrigin.transform.localPosition = Vector3.zero;

            xrOrigin.transform.localRotation = Quaternion.identity;
        }

        isRiding = true;

        speedTimer = 0f;
        whipCount = 0;
        currentSpeed = 0f;
        targetSpeed = 0f;
        whipCooldownTimer = 0f;

        if (GameUI.Instance) GameUI.Instance.HideText(this);
    }

    public void DismountHorse()
    {
        ResetReadyState();

        ForceReleaseRein(leftManeRein);
        ForceReleaseRein(rightManeRein);
        ForceReleaseRein(leftSaddleRein);
        ForceReleaseRein(rightSaddleRein);

        if (leftManeRein != null) leftManeRein.transform.SetLocalPositionAndRotation(initialLeftManeReinPos, initialLeftManeReinRot);
        if (rightManeRein != null) rightManeRein.transform.SetLocalPositionAndRotation(initialRightManeReinPos, initialRightManeReinRot);
        if (leftSaddleRein != null) leftSaddleRein.transform.SetLocalPositionAndRotation(initialLeftSaddleReinPos, initialLeftSaddleReinRot);
        if (rightSaddleRein != null) rightSaddleRein.transform.SetLocalPositionAndRotation(initialRightSaddleReinPos, initialRightSaddleReinRot);

        if (xrOrigin != null)
        {
            xrOrigin.transform.SetParent(null);
            if (exitPoint != null) xrOrigin.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

            // XR Origin üzerindeki CharacterController at indikten sonra tekrar aktifleþtiriliyor
            if (xrCharacterController != null) xrCharacterController.enabled = true;
            if (locomotionObject != null) locomotionObject.SetActive(true);
        }

        isRiding = false;

        targetSpeed = 0f;
        whipCount = 0;
        speedTimer = 0f;

        if (GameUI.Instance) GameUI.Instance.HideText(this);
    }

    private void ForceReleaseRein(XRBaseInteractable rein)
    {
        if (rein == null) return;

        if (rein.isSelected && rein.interactionManager != null)
        {
            var interactor = rein.firstInteractorSelecting;
            if (interactor != null) rein.interactionManager.SelectExit(interactor, rein);
        }
    }

    private void PreventInteractionOutsideRiding()
    {
        if (leftManeRein != null && leftManeRein.isSelected) ForceReleaseRein(leftManeRein);
        if (rightManeRein != null && rightManeRein.isSelected) ForceReleaseRein(rightManeRein);
        if (leftSaddleRein != null && leftSaddleRein.isSelected) ForceReleaseRein(leftSaddleRein);
        if (rightSaddleRein != null && rightSaddleRein.isSelected) ForceReleaseRein(rightSaddleRein);
    }

    private void SmoothResetReins()
    {
        ResetSingleRein(leftManeRein, initialLeftManeReinPos, initialLeftManeReinRot);
        ResetSingleRein(rightManeRein, initialRightManeReinPos, initialRightManeReinRot);
        ResetSingleRein(leftSaddleRein, initialLeftSaddleReinPos, initialLeftSaddleReinRot);
        ResetSingleRein(rightSaddleRein, initialRightSaddleReinPos, initialRightSaddleReinRot);
    }

    private void ResetSingleRein(XRBaseInteractable rein, Vector3 initialPos, Quaternion initialRot)
    {
        if (rein != null && !rein.isSelected)
        {
            rein.transform.SetLocalPositionAndRotation(
                Vector3.Lerp(rein.transform.localPosition, initialPos, Time.deltaTime * reinReturnSpeed),
                Quaternion.Slerp(rein.transform.localRotation, initialRot, Time.deltaTime * reinReturnSpeed)
            );
        }
    }

    public void ProcessPettingInteraction(Collider other)
    {
        if (allowedHandColliders == null || allowedHandColliders.Length == 0) return;

        bool isAllowed = false;
        for (int i = 0; i < allowedHandColliders.Length; i++)
        {
            if (allowedHandColliders[i] != null && allowedHandColliders[i] == other)
            {
                isAllowed = true;
                break;
            }
        }

        if (!isAllowed) return;

        bool isLeft = other.name.ToLower().Contains("left");
        Vector3 previousHandPos = isLeft ? lastLeftHandPetPos : lastRightHandPetPos;

        if (previousHandPos == Vector3.zero)
        {
            if (isLeft) lastLeftHandPetPos = other.transform.position;
            else lastRightHandPetPos = other.transform.position;
            return;
        }

        float deltaTime = Time.deltaTime > 0 ? Time.deltaTime : 0.011f;
        float handSpeed = (other.transform.position - previousHandPos).magnitude / deltaTime;

        if (Time.time - lastPetTime >= pettingCooldown && handSpeed >= minHandVelocityForPet)
        {
            TriggerPettingEffects();
        }

        if (isLeft) lastLeftHandPetPos = other.transform.position;
        else lastRightHandPetPos = other.transform.position;
    }

    private void TriggerPettingEffects()
    {
        lastPetTime = Time.time;

        if (horseAnimator != null) horseAnimator.SetTrigger("Pet");

        if (pettingSounds != null && pettingSounds.Length > 0 && horseAudioSource != null)
        {
            AudioClip randomPetSound = pettingSounds[UnityEngine.Random.Range(0, pettingSounds.Length)];
            horseAudioSource.PlayOneShot(randomPetSound, 0.75f);
        }

        if (pettingParticles != null) pettingParticles.Play();
    }

    private void PlayRandomCallingSound()
    {
        AudioSource sourceToUse = callingAudioSource != null ? callingAudioSource : horseAudioSource;
        if (sourceToUse == null || callingAudioClips == null || callingAudioClips.Length == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, callingAudioClips.Length);
        AudioClip clipToPlay = callingAudioClips[randomIndex];

        if (clipToPlay != null) sourceToUse.PlayOneShot(clipToPlay, 1f);
    }

    /// <summary>
    /// Inspector üzerinde 'None' býrakýlmýþ referanslarý sahnede arayýp otomatik doldurur.
    /// Doldurulmuþ referanslara dokunmaz.
    /// </summary>
    /// <summary>
    /// Inspector üzerinde 'None' býrakýlmýþ referanslarý sahnede arayýp otomatik doldurur.
    /// Doldurulmuþ referanslara dokunmaz.
    /// </summary>
    private void AutoAssignMissingReferences()
    {
        // 1. XR Origin
        if (xrOrigin == null)
        {
            xrOrigin = FindAnyObjectByType<XROrigin>();
        }

        // 2. Race Manager
        if (raceManager == null)
        {
            raceManager = FindAnyObjectByType<RaceManager>();
        }

        // 3. Locomotion Object
        if (locomotionObject == null)
        {
            GameObject locObj = GameObject.Find("Locomotion");
            if (locObj != null)
            {
                locomotionObject = locObj;
            }
            else if (xrOrigin != null)
            {
                Transform locTransform = xrOrigin.transform.Find("Locomotion");
                if (locTransform != null) locomotionObject = locTransform.gameObject;
            }
        }

        // 4. Hand Animators (Pasif olanlarý da güvenli þekilde arar)
        if (leftHandAnimator == null || rightHandAnimator == null)
        {
            // FindObjectsInactive.Include seçeneði sayesinde pasif objeleri de arar, ancak Prefab asset'lerine DOKUNMAZ.
            AnimateHandOnInput[] hands = FindObjectsByType<AnimateHandOnInput>(FindObjectsInactive.Include);

            foreach (var hand in hands)
            {
                string objectName = hand.gameObject.name;

                if (leftHandAnimator == null && objectName.Contains("Left Hand Model"))
                {
                    leftHandAnimator = hand;
                }
                else if (rightHandAnimator == null && objectName.Contains("Right Hand Model"))
                {
                    rightHandAnimator = hand;
                }
            }
        }

        // 5. Ready UI Elemanlarý (Güvenli Pasif Obje Arama)
        if (readyProgressBar == null)
        {
            UnityEngine.UI.Image[] images = FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include);
            foreach (var img in images)
            {
                if (img.gameObject.name == "ReadyProgressBar")
                {
                    readyProgressBar = img;
                    break;
                }
            }
        }

        if (readyUIContainer == null)
        {
            Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in allTransforms)
            {
                if (t.gameObject.name == "ReadyUIContainer")
                {
                    readyUIContainer = t.gameObject;
                    break;
                }
            }
        }

        // 6. Transform Noktalarý (Atýn kendi içindeki Child objeler)
        if (saddleSeatPoint == null)
        {
            Transform seat = transform.Find("SeatPoint");
            if (seat == null)
            {
                seat = transform.Find("SaddleSeatPoint");
            }
            if (seat != null) saddleSeatPoint = seat;
        }

        if (exitPoint == null)
        {
            Transform exit = transform.Find("ExitPoint");
            if (exit != null) exitPoint = exit;
        }

        if (groundCheckPoint == null)
        {
            Transform groundPoint = transform.Find("GroundCheckPoint");
            if (groundPoint != null) groundCheckPoint = groundPoint;
        }

        if (reinsAnchorPoint == null)
        {
            Transform anchor = transform.Find("ReinsAnchorPoint");
            if (anchor != null) reinsAnchorPoint = anchor;
        }

        // 7. Animator & AudioSource
        if (horseAnimator == null)
        {
            horseAnimator = GetComponentInChildren<Animator>();
        }

        if (horseAudioSource == null)
        {
            horseAudioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        mountHorseButton.action?.Enable();
        jumpHorseButton.action?.Enable();
        backKickButton.action?.Enable();
        rearUpButton.action?.Enable();
        maneControls.action?.Enable();
        leftTriggerAction.action?.Enable();
        rightTriggerAction.action?.Enable();
        leftControllerPositionAction.action?.Enable();
        rightControllerPositionAction.action?.Enable();
    }

    private void OnDisable()
    {
        mountHorseButton.action?.Disable();
        jumpHorseButton.action?.Disable();
        backKickButton.action?.Disable();
        rearUpButton.action?.Disable();
        maneControls.action?.Disable();
        leftTriggerAction.action?.Disable();
        rightTriggerAction.action?.Disable();
        leftControllerPositionAction.action?.Disable();
        rightControllerPositionAction.action?.Disable();
    }
}