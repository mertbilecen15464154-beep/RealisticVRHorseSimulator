using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(CharacterController))]
public class DirectGrabDetector : MonoBehaviour
{
    [Header("Horse Reference")]
    [SerializeField] private HorseControllerVR horseController;

    [Header("XRI 3.0 Component References")]
    [Tooltip("Sol El / Sol Interactor")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;

    [Tooltip("Sað El / Sað Interactor")]
    [SerializeField] private NearFarInteractor rightNearFarInteractor;

    [SerializeField] private XRPokeInteractor pokeInteractor;

    [Header("Player Movement Settings")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private LayerMask grabLayerMask = ~0;

    private CharacterController m_CharacterController;
    private readonly List<IXRInteractable> m_ValidTargets = new();

    private float m_VerticalVelocity;
    private float m_RotationX = 0f;
    private float m_RotationY = 0f;

    private void Reset()
    {
        if (pokeInteractor == null)
            pokeInteractor = GetComponent<XRPokeInteractor>();
    }

    private void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. Hareket ve Zýplama
        HandlePlayerMovement();

        // 2. Kamera Bakýþý (Sað Týk + Fare)
        HandleMouseLook();

        // 3. Klavye / VR Kontrolcü Tuþ Atamalarý (Z, X, C, V, Space)
        HandleKeyboardVRInputs();

        // 4. VR Interactor Hedef Tespiti
        CheckDirectTargets(leftNearFarInteractor);
        CheckDirectTargets(rightNearFarInteractor);
    }

    /// <summary>
    /// Smooth WASD Hareketi, Yer Çekimi ve Space ile Zýplama
    /// </summary>
    private void HandlePlayerMovement()
    {
        // EÐER CHARACTER CONTROLLER KAPALIYSAVVEYA OYUNCU ATA BÝNMÝÞSE HAREKET KODUNU ÇALIÞTIRMA
        if (m_CharacterController == null || !m_CharacterController.enabled)
            return;

        Vector2 inputVector = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1f;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1f;
            if (Keyboard.current.aKey.isPressed) inputVector.x -= 1f;
            if (Keyboard.current.dKey.isPressed) inputVector.x += 1f;
        }

        inputVector = inputVector.normalized;
        Vector3 move = transform.right * inputVector.x + transform.forward * inputVector.y;

        if (m_CharacterController.isGrounded)
        {
            if (m_VerticalVelocity < 0)
            {
                m_VerticalVelocity = -2f;
            }
        }

        move.y = m_VerticalVelocity;

        // 98. SATIR: Artýk sadece CharacterController aktifse çalýþacak
        m_CharacterController.Move(moveSpeed * Time.deltaTime * move);
    }

    /// <summary>
    /// Fare ile Bakýþ Açýsýný Çevirme (Sað Týk Basýlý Tutunca)
    /// </summary>
    private void HandleMouseLook()
    {
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            m_RotationY += mouseDelta.x * mouseSensitivity * 0.1f;
            m_RotationX -= mouseDelta.y * mouseSensitivity * 0.1f;
            m_RotationX = Mathf.Clamp(m_RotationX, -85f, 85f);

            transform.localRotation = Quaternion.Euler(m_RotationX, m_RotationY, 0f);
        }
    }

    /// <summary>
    /// Z, X (Sol El) ve C, V (Sað El) Tuþ Atamalarý
    /// </summary>
    private void HandleKeyboardVRInputs()
    {
        if (Keyboard.current == null) return;

        // E Tuþuna basýldýðýnda ata bin / attan in
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (horseController != null)
            {
                if (horseController.isRiding)
                {
                    horseController.DismountHorse();
                }
                else
                {
                    // Mesafe kontrolü olmaksýzýn doðrudan binmek için:
                    horseController.MountHorse();
                }
            }
            else
            {
                Debug.LogError("HorseControllerVR referansý Inspector üzerinde boþ (None)!");
            }
        }

        if (Keyboard.current == null) return;

        // --- SOL EL KONTROLLERÝ ---
        // Z Tuþu -> Sol Grip (Tutma / Grab)
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            ExecuteHandAction(leftNearFarInteractor, "Sol El Grip (Z)");
        }

        // X Tuþu -> Sol Trigger (Tetik / Primary Interact)
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            ExecuteHandAction(leftNearFarInteractor, "Sol El Trigger (X)");
        }

        // --- SAÐ EL KONTROLLERÝ ---
        // C Tuþu -> Sað Grip (Tutma / Grab)
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ExecuteHandAction(rightNearFarInteractor, "Sað El Grip (C)");
        }

        // V Tuþu -> Sað Trigger (Tetik / Primary Interact)
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            ExecuteHandAction(rightNearFarInteractor, "Sað El Trigger (V)");
        }
    }

    /// <summary>
    /// Ýlgili Interactor üzerinden etkileþimi tetikler
    /// </summary>
    private void ExecuteHandAction(NearFarInteractor interactor, string actionName)
    {
        if (interactor == null || !interactor.isActiveAndEnabled) return;

        m_ValidTargets.Clear();
        interactor.GetValidTargets(m_ValidTargets);

        if (m_ValidTargets.Count > 0 && m_ValidTargets[0] is IXRSelectInteractable selectInteractable)
        {
            Debug.Log($"{actionName} tetiklendi -> Nesne: {selectInteractable.transform.name}");
            OnTargetDetected(selectInteractable);
        }
        else
        {
            Debug.Log($"{actionName} tetiklendi -> Menzilde Etkileþime Girilecek Nesne Yok.");
        }
    }

    public void CheckDirectTargets(NearFarInteractor interactor)
    {
        if (interactor == null || !interactor.isActiveAndEnabled) return;

        m_ValidTargets.Clear();
        interactor.GetValidTargets(m_ValidTargets);

        foreach (var target in m_ValidTargets)
        {
            if (target is IXRSelectInteractable selectInteractable)
            {
                if (((1 << selectInteractable.transform.gameObject.layer) & grabLayerMask) != 0)
                {
                    OnTargetDetected(selectInteractable);
                }
            }
        }
    }

    private void OnTargetDetected(IXRSelectInteractable _)
    {
        // Nesne algýlandýðýnda yapýlacak iþlemler
    }
}