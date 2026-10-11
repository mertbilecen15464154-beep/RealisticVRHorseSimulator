using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HorseAIController : MonoBehaviour
{
    [Header("AI Waypoint & Target Settings")]
    public float waypointNodeRadius = 2.5f; // Checkpoint'e ulaþýldýðýnda geçiþ için yarýçap
    public float minRandomOffset = -1.2f; // Checkpoint hedef pozisyonuna eklenen rastgele offsetin minimum deðeri
    public float maxRandomOffset = 1.2f; // Checkpoint hedef pozisyonuna eklenen rastgele offsetin maksimum deðeri

    private Vector3 currentTargetOffset;
    private int currentWaypointIndex = 0;

    [Header("Movement Speeds & AI Target")]
    public float walkSpeed = 3f;
    public float trotSpeed = 6f;
    public float gallopSpeed = 12f;
    public float acceleration = 4f;
    public float rotateSpeed = 50f;
    public float minRandomSpeed = 10f;
    public float maxRandomSpeed = 12.5f;
    private float targetMaxSpeed;

    [Header("Grounded & Raycast Settings")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.8f;
    public Transform groundCheckPoint;
    public bool isGrounded = false;

    [Header("Custom Gravity Settings")]
    public float jumpGravity = -10f;
    public float fallGravity = -30f;
    private float verticalVelocity = 0f;

    [Header("Fall & Land Settings")]
    public float fallVelocityThreshold = -2.5f;

    [Header("Impact & Collision Settings")]
    public LayerMask obstacleLayers = ~0;
    public float impactCooldown = 1.5f;

    [Header("Impact Knockback Settings")]
    public float knockbackMultiplier = 0.6f;
    public float maxKnockbackSpeed = 8f;

    [Header("AI Jump Settings")]
    public float jumpForce = 8f;
    public float obstacleCheckDistance = 4f;
    public float jumpCooldown = 2.0f;
    private float lastJumpTime = -99f;

    [Header("Animation Settings")]
    public Animator horseAnimator;
    public float idleTransitionSpeed = 2f;

    [Header("Audio Settings - General")]
    public AudioSource horseAudioSource;
    public AudioSource footstepAudioSource;
    public AudioClip jumpSound;
    public AudioClip[] knockbackSounds;
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

    private float currentSpeed = 0f;
    private float lastImpactTime = -99f;

    private float currentTurnAnim = 0f;
    private float currentSpeedAnim = 0f;
    private float targetIdleIndex = 0f;
    private float currentIdleIndexAnim = 0f;
    private float rawTurnInput = 0f;

    private bool isWaitingInMainIdle = true;
    private bool hasPlayedIdleSound = false;
    private float lastFootstepTime = 0f;
    private const float FOOTSTEP_DEBOUNCE_TIME = 0.15f;

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");

    void Start()
    {
        AutoAssignMissingReferences();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Custom gravity kullanýyoruz

        targetMaxSpeed = UnityEngine.Random.Range(minRandomSpeed, maxRandomSpeed);

        SetNewRandomOffset();

        // AI Atýn kendi kemik collider'larýnýn birbiriyle çakýþýp fýrlamasýný engelle
        Collider[] allBoneColliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < allBoneColliders.Length; i++)
        {
            for (int j = i + 1; j < allBoneColliders.Length; j++)
            {
                if (allBoneColliders[i] != null && allBoneColliders[j] != null)
                {
                    Physics.IgnoreCollision(allBoneColliders[i], allBoneColliders[j], true);
                }
            }
        }

        targetIdleIndex = 0f;
        currentIdleIndexAnim = 0f;

        if (horseAudioSource == null) horseAudioSource = gameObject.AddComponent<AudioSource>();
        if (footstepAudioSource == null) footstepAudioSource = gameObject.AddComponent<AudioSource>();

        horseAudioSource.spatialBlend = 1f;
        footstepAudioSource.spatialBlend = 1f;
    }

    private bool isFinished = false;
    private int aiCurrentLap = 1;

    void Update()
    {
        CheckGround();
        HandleFallAndLandState();

        bool isRaceActive = RaceManager.Instance != null && RaceManager.Instance.isRaceStarted;
        bool isStunnedOrRecovering = IsCustomAnimationActive();

        if (isRaceActive && !isFinished && !isStunnedOrRecovering)
        {
            NavigateCheckpoints();
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetMaxSpeed, Time.deltaTime * acceleration);
            CheckObstacleAndJump();
        }
        else
        {
            // Tur bittiðinde veya yarýþ aktif deðilse yavaþlayýp tamamen dur
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * 6f);
            rawTurnInput = 0f;

            if (!isRaceActive)
            {
                currentWaypointIndex = 0;
                aiCurrentLap = 1;
                isFinished = false;
            }
        }

        UpdateAnimations();
        UpdateIdleSystems();
    }

    // AI son checkpoint'i geçip yarýþý/turu tamamladýðýnda çaðrýlacak metot
    public void SetFinished()
    {
        isFinished = true;
    }

    private void FixedUpdate()
    {
        ApplyPhysicsMovement();
    }

    private void SetNewRandomOffset()
    {
        float randomX = UnityEngine.Random.Range(minRandomOffset, maxRandomOffset);
        float randomZ = UnityEngine.Random.Range(minRandomOffset, maxRandomOffset);
        currentTargetOffset = new Vector3(randomX, 0f, randomZ);
    }

    private void NavigateCheckpoints()
    {
        if (RaceManager.Instance == null || RaceManager.Instance.checkpointsParent == null) return;

        Transform checkpointsParent = RaceManager.Instance.checkpointsParent;
        int totalCheckpoints = checkpointsParent.childCount;
        if (totalCheckpoints == 0) return;

        Transform targetNode = checkpointsParent.GetChild(currentWaypointIndex);
        Vector3 targetPos = targetNode.position + currentTargetOffset;

        Vector3 flatSelf = new(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new(targetPos.x, 0f, targetPos.z);

        float distance = Vector3.Distance(flatSelf, flatTarget);

        if (distance <= waypointNodeRadius)
        {
            currentWaypointIndex++;

            // AI pistin tam bir turunu (tüm checkpoint'leri) bitirdi mi?
            if (currentWaypointIndex >= totalCheckpoints)
            {
                currentWaypointIndex = 0;

                // AI toplam tura ulaþtý mý kontrol et
                if (aiCurrentLap >= RaceManager.Instance.totalLaps)
                {
                    isFinished = true;
                    RaceManager.Instance.OnAiFinished(this);
                    return;
                }
                else
                {
                    aiCurrentLap++;
                }
            }

            SetNewRandomOffset();
            targetNode = checkpointsParent.GetChild(currentWaypointIndex);
            targetPos = targetNode.position + currentTargetOffset;
            flatTarget = new Vector3(targetPos.x, 0f, targetPos.z);
        }

        Vector3 targetDirection = (flatTarget - flatSelf).normalized;
        if (targetDirection != Vector3.zero)
        {
            float angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
            rawTurnInput = Mathf.Clamp(angle / 30f, -1f, 1f);
        }
    }

    private Vector3 groundNormal = Vector3.up;

    private void CheckGround()
    {
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.up * 0.5f;

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

    // Dýþarýdan veya yönlendirme sisteminden direksiyon girdisi vermek istersen kullanýlabilir
    public void SetTurnInput(float turnInput)
    {
        rawTurnInput = Mathf.Clamp(turnInput, -1f, 1f);
    }

    private void CheckObstacleAndJump()
    {
        if (!isGrounded || Time.time - lastJumpTime < jumpCooldown) return;

        // Hýza göre algýlama mesafesini dinamik hesapla (Mevcut hýz arttýkça daha uzaktan engeli fark eder)
        float dynamicCheckDistance = Mathf.Max(obstacleCheckDistance, currentSpeed * 0.45f);

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Ýleriye doðru tek ray yerine biraz daha geniþ açý algýlama için RaycastHit kontrolü
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, dynamicCheckDistance, obstacleLayers))
        {
            // Dik engelleri tespit et
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.5f)
            {
                ExecuteJump();
            }
        }
        else
        {
            // Göðüs hizasýndaki düþük/yüksek engel kaçýrmalarýný önlemek için ikinci bir Raycast
            Vector3 chestOrigin = transform.position + Vector3.up * 1.0f;
            if (Physics.Raycast(chestOrigin, transform.forward, out RaycastHit chestHit, dynamicCheckDistance, obstacleLayers))
            {
                if (Vector3.Dot(chestHit.normal, Vector3.up) < 0.5f)
                {
                    ExecuteJump();
                }
            }
        }
    }

    private void ExecuteJump()
    {
        lastJumpTime = Time.time;

        // Hýzla orantýlý dinamik zýplama kuvveti (Hýzlý giderken daha yüksek ve ileri momentumlu zýplar)
        float speedRatio = Mathf.Clamp01(currentSpeed / maxRandomSpeed);
        float dynamicJumpForce = Mathf.Lerp(jumpForce, jumpForce * 1.35f, speedRatio);

        verticalVelocity = dynamicJumpForce;

        if (horseAnimator != null) horseAnimator.SetTrigger("Jump");
        if (jumpSound != null && horseAudioSource != null) horseAudioSource.PlayOneShot(jumpSound, 0.8f);
    }

    private void ApplyPhysicsMovement()
    {
        if (rb == null) return;

        // Yer Çekimi ve Zemin Mantýðý
        if (isGrounded)
        {
            if (verticalVelocity <= 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += jumpGravity * Time.fixedDeltaTime;
            }
        }
        else
        {
            if (verticalVelocity > 0f)
            {
                verticalVelocity += jumpGravity * Time.fixedDeltaTime;
            }
            else
            {
                verticalVelocity += fallGravity * Time.fixedDeltaTime;
            }
        }

        // Ýleri Hareket Vektörü
        Vector3 moveDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
        Vector3 targetMoveVelocity = moveDirection * currentSpeed;

        targetMoveVelocity.y = verticalVelocity;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetMoveVelocity, Time.fixedDeltaTime * 10f);

        // Dönüþ Mantýðý
        if (Mathf.Abs(currentSpeed) > 0.1f && Mathf.Abs(rawTurnInput) > 0.02f)
        {
            float directionMultiplier = currentSpeed > 0f ? 1f : -1f;
            float actualRotateSpeed = rotateSpeed * Mathf.Clamp01(Mathf.Abs(currentSpeed) / walkSpeed);

            Quaternion deltaRotation = Quaternion.Euler(0f, rawTurnInput * directionMultiplier * actualRotateSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
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
        if (((1 << collision.gameObject.layer) & obstacleLayers) == 0) return;

        ContactPoint contact = collision.contacts[0];
        if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f) return;

        bool isGalloping = currentSpeed >= (trotSpeed + 0.5f);

        if (isGalloping && Time.time - lastImpactTime >= impactCooldown)
        {
            TriggerHorseImpact();
        }
        else
        {
            if (!isGalloping)
            {
                currentSpeed = 0f;
            }
        }
    }

    private void TriggerHorseImpact()
    {
        lastImpactTime = Time.time;
        float calculatedKnockback = Mathf.Clamp(currentSpeed * knockbackMultiplier, 2f, maxKnockbackSpeed);

        currentSpeed = 0f;

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

    private void UpdateAnimations()
    {
        if (horseAnimator == null) return;

        float targetTurnValue = Mathf.Clamp(rawTurnInput, -1f, 1f);
        currentTurnAnim = Mathf.Lerp(currentTurnAnim, targetTurnValue, Time.deltaTime * 5f);
        horseAnimator.SetFloat("Turn", currentTurnAnim);

        bool isBackingValue = currentSpeed < -0.05f;
        horseAnimator.SetBool("IsBacking", isBackingValue);

        float speedParamForAnimator = isBackingValue ? 0f : Mathf.Max(0f, currentSpeed);

        currentSpeedAnim = Mathf.Lerp(currentSpeedAnim, speedParamForAnimator, Time.deltaTime * 6f);
        horseAnimator.SetFloat("Speed", currentSpeedAnim);

        currentIdleIndexAnim = Mathf.MoveTowards(currentIdleIndexAnim, targetIdleIndex, Time.deltaTime * idleTransitionSpeed);
        horseAnimator.SetFloat("IdleIndex", currentIdleIndexAnim);
    }

    private void UpdateIdleSystems()
    {
        if (horseAnimator == null) return;

        if (Mathf.Abs(currentSpeed) > 0.05f || IsCustomAnimationActive() || !isGrounded)
        {
            targetIdleIndex = 0f;
            isWaitingInMainIdle = true;
            hasPlayedIdleSound = false;
            return;
        }

        AnimatorStateInfo stateInfo = horseAnimator.GetCurrentAnimatorStateInfo(0);
        float animationProgress = stateInfo.normalizedTime % 1f;

        if (animationProgress >= 0.98f && !horseAnimator.IsInTransition(0))
        {
            if (isWaitingInMainIdle)
            {
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

    private void AutoAssignMissingReferences()
    {
        if (groundCheckPoint == null)
        {
            Transform groundPoint = transform.Find("GroundCheckPoint");
            if (groundPoint != null) groundCheckPoint = groundPoint;
        }

        if (horseAnimator == null)
        {
            horseAnimator = GetComponentInChildren<Animator>();
        }

        if (horseAudioSource == null)
        {
            horseAudioSource = GetComponent<AudioSource>();
        }
    }
}