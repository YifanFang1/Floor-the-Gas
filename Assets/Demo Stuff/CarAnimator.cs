using UnityEngine;

public class CarAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform visualModel;   
    public Transform[] wheels;      
    
    [Header("Settings")]
    public float stretchFactor = 0.01f; 
    public float wheelSpinSpeed = 10f;
    
    // NEW: Select "Y" in the Inspector for your rotated car!
    public Axis stretchAxis = Axis.Y; 
    public enum Axis { X, Y, Z }

    private Rigidbody rb;
    private float currentSquash = 1f;
    private Vector3 initialScale;
    private Vector3[] wheelInitialScales;
    private Vector3[] wheelInitialPositions;
    private SimpleDrive drive;

    void Start() {
        // Store initial positions for each wheel
        if (wheels != null)
        {
            wheelInitialPositions = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                    wheelInitialPositions[i] = wheels[i].localPosition;
            }
        }
        rb = GetComponent<Rigidbody>();
        drive = GetComponent<SimpleDrive>();
        // MEMORIZE: Keep your scale of 100 (or whatever it is)
        if (visualModel)
        {
            initialScale = visualModel.localScale;
        }
        // Store initial scales for each wheel
        if (wheels != null)
        {
            wheelInitialScales = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                    wheelInitialScales[i] = wheels[i].localScale;
            }
        }
    }

    void Update()
    {
        if (rb == null || visualModel == null) return;
        
        // 1. GET SPEED
        // If Unity gives an error here, change 'linearVelocity' to 'velocity'
        float speed = rb.linearVelocity.magnitude;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Only stretch if boosting
        bool boosting = false;
        if (drive != null)
        {
            // Use boostTimer for smoothness
            var driveType = drive.GetType();
            var boostTimerField = driveType.GetField("boostTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (boostTimerField != null)
            {
                float boostTimer = (float)boostTimerField.GetValue(drive);
                boosting = boostTimer > 0f;
            }
        }

        if (boosting)
        {
            // 2. CALCULATE SQUASH
            float targetStretch = 1f + (speed * stretchFactor);
            // Decrease the stretch speed for boost (was 5f)
            currentSquash = Mathf.Lerp(currentSquash, targetStretch, Time.deltaTime * 2f);
        }
        else
        {
            currentSquash = Mathf.Lerp(currentSquash, 1f, Time.deltaTime * 5f);
        }

        float s = currentSquash;       // The Stretch
        float q = 1f / Mathf.Sqrt(s);  // The Squash (Preserves Volume)

        // 3. APPLY TO CORRECT AXIS
        Vector3 stretchDir = Vector3.one;
        if (stretchAxis == Axis.X) stretchDir = new Vector3(s, q, q);
        if (stretchAxis == Axis.Y) stretchDir = new Vector3(q, s, q);
        if (stretchAxis == Axis.Z) stretchDir = new Vector3(q, q, s);

        visualModel.localScale = Vector3.Scale(initialScale, stretchDir);

        // 4. WHEEL DISTANCE EXPANSION & SPIN
        for (int i = 0; i < wheels.Length; i++)
        {
            Transform w = wheels[i];
            if (w)
            {
                // Move wheels outward based on stretch
                if (wheelInitialPositions != null && i < wheelInitialPositions.Length)
                {
                    Vector3 offset = wheelInitialPositions[i];
                    // Expand only along the stretch axis
                    if (stretchAxis == Axis.X)
                        offset.x *= s;
                    else if (stretchAxis == Axis.Y)
                        offset.y *= s;
                    else if (stretchAxis == Axis.Z)
                        offset.z *= s;
                    w.localPosition = offset;
                }
                // Keep wheel scale unchanged
                if (wheelInitialScales != null && i < wheelInitialScales.Length)
                    w.localScale = wheelInitialScales[i];
                // Rotate the WHEEL HOLDERS, not the mesh directly
                w.Rotate(forwardSpeed * wheelSpinSpeed * Time.deltaTime, 0, 0, Space.Self);
            }
        }
    }
}