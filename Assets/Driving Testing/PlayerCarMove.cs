using UnityEngine;

public class PlayerCarMove : MonoBehaviour
{
    private Rigidbody _rb;

    [Header("Wheels Setup (Assign empty GameObjects)")]
    [SerializeField]
    private Transform[] wheelRaycastPoints = new Transform[4]; // Front-Left, Front-Right, Rear-Left, Rear-Right
    [SerializeField]
    private LayerMask trackLayer;

    [Header("Suspension Settings")]
    [SerializeField]
    private float suspensionRestDist = 0.5f; // How high the car rides
    [SerializeField]
    private float springStrength = 30000f;
    [SerializeField]
    private float springDamper = 3000f;

    [Header("Drive Settings")]
    [SerializeField]
    private float acceleration = 60f;
    [SerializeField]
    private float topSpeed = 120f;
    [SerializeField]
    private float turnSpeed = 80f;

    [Header("Boost Settings (Space)")]
    [SerializeField]
    private float boostMultiplier = 1.8f;

    [Header("Drift Settings (Shift)")]
    [Tooltip("Normal grip (1 = on rails). Drift grip (lower = sliding).")]
    [SerializeField]
    private float normalGrip = 0.95f;
    [SerializeField]
    private float driftGrip = 0.3f;
    [SerializeField]
    private float driftTurnMultiplier = 1.5f; // Turn sharper when drifting

    [Header("Visuals (Optional)")]
    [SerializeField]
    private Transform carBodyMesh; // To tilt the car when turning

    // Input States
    private float _moveInput;
    private float _turnInput;
    private bool _isBoosting;
    private bool _isDrifting;
    private int _wheelsGrounded;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // Lower center of mass so the car doesn't flip on corners
        _rb.centerOfMass = new Vector3(0, -0.5f, 0); 
    }

    void Update()
    {
        // Gather Inputs
        _moveInput = Input.GetAxis("Vertical");
        _turnInput = Input.GetAxis("Horizontal");
        _isBoosting = Input.GetKey(KeyCode.Space);
        _isDrifting = Input.GetKey(KeyCode.LeftShift);

        // Visual Body Roll (Tilt car when steering)
        if (carBodyMesh != null)
        {
            float tilt = -_turnInput * (_isDrifting ? 15f : 5f);
            carBodyMesh.localRotation = Quaternion.Slerp(carBodyMesh.localRotation, Quaternion.Euler(0, 0, tilt), Time.deltaTime * 10f);
        }
    }

    void FixedUpdate()
    {
        _wheelsGrounded = 0;

        // 1. Process 4-Wheel Suspension
        foreach (Transform wheelPoint in wheelRaycastPoints)
        {
            if (Physics.Raycast(wheelPoint.position, -transform.up, out RaycastHit hit, suspensionRestDist + 0.5f, trackLayer))
            {
                _wheelsGrounded++;
                
                // Calculate Spring Force
                Vector3 springDir = transform.up;
                float offset = suspensionRestDist - hit.distance;
                float vel = Vector3.Dot(springDir, _rb.GetPointVelocity(wheelPoint.position));
                float force = (offset * springStrength) - (vel * springDamper);
                
                _rb.AddForceAtPosition(springDir * force, wheelPoint.position);
            }
        }

        // If at least one wheel is touching the ground, we can drive
        if (_wheelsGrounded > 0)
        {
            HandleAcceleration();
            HandleSteering();
            ApplyGrip();

            // Fake Gravity: Push the car down against the track normal to stick to loops/ramps
            _rb.AddForce(-transform.up * 20f, ForceMode.Acceleration);
        }
        else
        {
            // Standard falling gravity
            _rb.AddForce(Vector3.down * 40f, ForceMode.Acceleration);
        }
    }

    private void HandleAcceleration()
    {
        float currentSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float activeTopSpeed = _isBoosting ? topSpeed * boostMultiplier : topSpeed;
        float activeAccel = _isBoosting ? acceleration * 1.5f : acceleration;

        if (Mathf.Abs(currentSpeed) < activeTopSpeed)
        {
            // Apply force at the center of the car
            _rb.AddForce(transform.forward * _moveInput * activeAccel, ForceMode.Acceleration);
        }
    }

    private void HandleSteering()
    {
        // You can only turn if you are moving forward/backward
        float speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / 15f); 
        float activeTurnSpeed = _isDrifting ? turnSpeed * driftTurnMultiplier : turnSpeed;
        
        // Reverse steering logic when driving backwards
        float turnDirection = Vector3.Dot(_rb.linearVelocity, transform.forward) >= 0 ? 1f : -1f;

        _rb.AddTorque(transform.up * _turnInput * activeTurnSpeed * speedFactor * turnDirection, ForceMode.Acceleration);
    }

    private void ApplyGrip()
    {
        // Cancel out sideways velocity to simulate tire friction
        Vector3 rightVelocity = transform.right * Vector3.Dot(_rb.linearVelocity, transform.right);
        
        float currentGrip = _isDrifting ? driftGrip : normalGrip;

        _rb.AddForce(-rightVelocity * currentGrip, ForceMode.VelocityChange);
    }
}
