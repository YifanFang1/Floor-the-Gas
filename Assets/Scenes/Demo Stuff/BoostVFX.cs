using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BoostVFX : MonoBehaviour
{
    [Header("References")]
    public Volume globalVolume; // Drag your Global Volume here

    [Header("Chromatic Aberration (The Blur)")]
    public float normalBlur = 0f;
    public float boostBlur = 1f; // Max is usually 1

    [Header("Lens Distortion (The Tunnel)")]
    public float normalDistortion = 0f;
    public float boostDistortion = -0.5f; // Negative stretches corners

    private ChromaticAberration ca;
    private LensDistortion ld;
    private bool isBoosting;
    private float lastForwardSpeed;
    private float accelerationValue;

    void Start()
    {
        // Find the effects inside the Volume Profile
        if (globalVolume.profile.TryGet(out ca) == false)
        {
            Debug.LogWarning("Chromatic Aberration is missing from your Global Volume!");
        }
        if (globalVolume.profile.TryGet(out ld) == false)
        {
            Debug.LogWarning("Lens Distortion is missing from your Global Volume!");
        }
    }

    void Update()
    {
        // Get acceleration from SimpleDrive if present
        float currentForwardSpeed = 0f;
        var drive = GetComponent<SimpleDrive>();
        if (drive != null && drive.GetComponent<Rigidbody>() != null)
        {
            var rb = drive.GetComponent<Rigidbody>();
            currentForwardSpeed = Vector3.Dot(rb.linearVelocity, drive.transform.forward);
        }
        accelerationValue = (currentForwardSpeed - lastForwardSpeed) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastForwardSpeed = currentForwardSpeed;

        // 1. Check for Boost Input (Space)
        isBoosting = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;

        // 2. Lerp Values
        float lerpSpeed = 5f * Time.deltaTime;

        if (ca != null)
        {
            float targetBlur = isBoosting ? boostBlur : normalBlur;
            ca.intensity.value = Mathf.Lerp(ca.intensity.value, targetBlur, lerpSpeed);
        }

        if (ld != null)
        {
            // Ramp up distortion with acceleration
            float accelT = Mathf.InverseLerp(0f, 60f, Mathf.Max(accelerationValue, 0f)); // 0 to 60+ accel
            float targetDist = Mathf.Lerp(normalDistortion, boostDistortion, accelT);
            ld.intensity.value = Mathf.Lerp(ld.intensity.value, targetDist, lerpSpeed);
            // Optional: Zoom out slightly while boosting to enhance speed feel
            // ld.scale.value = Mathf.Lerp(ld.scale.value, isBoosting ? 0.9f : 1f, lerpSpeed);
        }
    }
}
