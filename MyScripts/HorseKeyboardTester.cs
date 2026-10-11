using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HorseControllerVR))]
public class HorseKeyboardTester : MonoBehaviour
{
    private HorseControllerVR horseController;

    [Header("Keyboard Controls Helper")]
    [Tooltip("Sadece Editörde veya Debug modunda çalýþmasýný istiyorsanýz iþaretleyin.")]
    public bool enableKeyboardTesting = true;

    void Awake()
    {
        horseController = GetComponent<HorseControllerVR>();
    }

    void Update()
    {
        if (!enableKeyboardTesting || horseController == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. Ata Binme / Attan Ýnme (E Tuþu)
        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (horseController.isRiding)
            {
                horseController.DismountHorse();
            }
            else
            {
                // Yakýnlýk kontrolü
                if (horseController.xrOrigin != null)
                {
                    float dist = Vector3.Distance(horseController.xrOrigin.transform.position, transform.position);
                    if (dist <= horseController.interactionDistance)
                    {
                        horseController.MountHorse();
                    }
                }
                else
                {
                    horseController.MountHorse();
                }
            }
        }

        // Sadece ata biniliyken çalýþacak test kontrolleri
        if (!horseController.isRiding) return;

        // 2. Kýrbaçlama / Hýzlanma (W veya Sol Shift)
        if (keyboard.wKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame)
        {
            // Atýn hýz kademesini (Whip) simüle eder
            SimulateWhip();
        }

        // 3. Zýplama (Space)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            SimulateJump();
        }

        // 4. Þahlanma (R Tuþu)
        if (keyboard.rKey.wasPressedThisFrame)
        {
            SimulateTrigger("RearUp", horseController.rearUpSounds);
        }

        // 5. Arka Çifte / Saldýrý (F Tuþu)
        if (keyboard.fKey.wasPressedThisFrame)
        {
            SimulateTrigger("BackAttack", horseController.backAttackSounds);
        }
    }

    private void SimulateWhip()
    {
        // Reflection kullanarak HorseControllerVR içindeki özel whip/hýz deðiþkenlerini günceller
        var fieldWhipCount = typeof(HorseControllerVR).GetField("whipCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fieldSpeedTimer = typeof(HorseControllerVR).GetField("speedTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fieldTargetSpeed = typeof(HorseControllerVR).GetField("targetSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldWhipCount != null && fieldSpeedTimer != null && fieldTargetSpeed != null)
        {
            int currentWhip = (int)fieldWhipCount.GetValue(horseController);
            currentWhip = Mathf.Clamp(currentWhip + 1, 1, 3);
            fieldWhipCount.SetValue(horseController, currentWhip);

            fieldSpeedTimer.SetValue(horseController, horseController.speedDuration);

            float speed = currentWhip switch
            {
                1 => horseController.walkSpeed,
                2 => horseController.trotSpeed,
                3 => horseController.gallopSpeed,
                _ => 0f
            };

            fieldTargetSpeed.SetValue(horseController, speed);

            // Kýrbaç sesini çal
            typeof(HorseControllerVR).GetMethod("PlayWhipSound", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(horseController, null);
        }
    }

    private void SimulateJump()
    {
        var controller = horseController.GetComponent<CharacterController>();
        if (controller != null && controller.isGrounded)
        {
            var fieldVertVel = typeof(HorseControllerVR).GetField("verticalVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldVertVel?.SetValue(horseController, horseController.jumpForce);

            if (horseController.horseAnimator != null)
            {
                horseController.horseAnimator.SetTrigger("Jump");
            }

            if (horseController.jumpSound != null && horseController.horseAudioSource != null)
            {
                horseController.horseAudioSource.PlayOneShot(horseController.jumpSound, 0.8f);
            }
        }
    }

    private void SimulateTrigger(string triggerName, AudioClip[] sounds)
    {
        var fieldSpeed = typeof(HorseControllerVR).GetField("currentSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float currentSpeed = fieldSpeed != null ? (float)fieldSpeed.GetValue(horseController) : 0f;

        var controller = horseController.GetComponent<CharacterController>();
        if (Mathf.Abs(currentSpeed) < 0.1f && controller != null && controller.isGrounded)
        {
            if (horseController.horseAnimator != null)
            {
                horseController.horseAnimator.SetTrigger(triggerName);
            }

            if (sounds != null && sounds.Length > 0 && horseController.horseAudioSource != null)
            {
                AudioClip clip = sounds[Random.Range(0, sounds.Length)];
                if (clip != null)
                {
                    horseController.horseAudioSource.PlayOneShot(clip, 1f);
                }
            }
        }
    }
}