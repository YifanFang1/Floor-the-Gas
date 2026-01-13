using UnityEngine;

public class ActionCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          
    
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 3, -6); 
    public float smoothSpeed = 5f;    
    public float rotationSpeed = 10f; 
    
    [Header("Speed FX")]
    public float baseFOV = 60f;
    public float maxFOV = 90f;
    
    [Header("Shake FX")]
    [Range(0, 1)] public float shakeIntensity = 0.5f; // Master slider
    public float vibrationSpeed = 20f; // How fast the shake oscillates
    
    private Rigidbody targetRb;
    private Camera cam;
    private float noiseOffset; // Random seed for Perlin noise

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target) targetRb = target.GetComponent<Rigidbody>();
        noiseOffset = Random.Range(0f, 100f);
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- 1. BASIC FOLLOW ---
        Vector3 desiredPos = target.TransformPoint(offset);
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // --- 2. CALCULATE SPEED ---
        // Unity 6: linearVelocity. Use .velocity for older Unity.
        // Only use horizontal (XZ) velocity for shake
        Vector3 horizontalVel = targetRb.linearVelocity;
        horizontalVel.y = 0f;
        float rawSpeed = horizontalVel.magnitude;
        // Make shake less sensitive at low speeds: only higher speeds trigger it
        float minShakeSpeed = 25f; // No shake below this speed
        float maxShakeSpeed = 60f; // Full shake at this speed
        float speedFactor = Mathf.Clamp01((rawSpeed - minShakeSpeed) / (maxShakeSpeed - minShakeSpeed));

        // --- PRE-BOOST SHAKE ---
        // If SimpleDrive is present, check for pre-boost state
        float preBoostShake = 0f;
        if (target != null)
        {
            var drive = target.GetComponent<SimpleDrive>();
            if (drive != null)
            {
                var driveType = drive.GetType();
                var isBoostPendingField = driveType.GetField("isBoostPending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var boostDelayTimerField = driveType.GetField("boostDelayTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var boostDelayField = driveType.GetField("boostDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isBoostPendingField != null && boostDelayTimerField != null && boostDelayField != null)
                {
                    bool isBoostPending = (bool)isBoostPendingField.GetValue(drive);
                    float boostDelayTimer = (float)boostDelayTimerField.GetValue(drive);
                    float boostDelay = (float)boostDelayField.GetValue(drive);
                    if (isBoostPending && boostDelay > 0f)
                    {
                        // Ramp up shake from 0 to 1 as timer counts down
                        preBoostShake = 1f - Mathf.Clamp01(boostDelayTimer / boostDelay);
                    }
                }
            }
        }

        // --- 3. APPLY SHAKE ---
        // We use Perlin Noise because it looks like natural vibration, not just random jitter.
        // The shake gets stronger (amplitude) as speed increases.
        
        // Use a higher exponent for shake: almost none at low speed, massive at high speed
        float shakePower = Mathf.Pow(6f, speedFactor) - 1f;
        shakePower *= shakeIntensity;
        // Add pre-boost shake (ramps up quickly)
        if (preBoostShake > 0f)
        {
            shakePower += preBoostShake * 2.5f * shakeIntensity; // Strong shake for pre-boost
        }

        // Only move camera up (not down) to avoid floor clipping
        float shakeNoiseX = Mathf.PerlinNoise(Time.time * vibrationSpeed, noiseOffset) - 0.5f;
        float shakeNoiseY = Mathf.Abs(Mathf.PerlinNoise(Time.time * vibrationSpeed, noiseOffset + 50f) - 0.5f); // Only positive
        Vector3 shakeOffset = new Vector3(shakeNoiseX, shakeNoiseY, 0) * shakePower;

        // Apply shake to the smoothed position
        transform.position = smoothedPos + shakeOffset;

        // --- 4. ROTATION ---
        Vector3 direction = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        // --- 5. FOV WARP ---
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedFactor);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }
}