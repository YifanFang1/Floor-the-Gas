#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleDrive : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 50f; 
    [SerializeField] private float acceleration = 8f; 
    [SerializeField] private float deceleration = 10f; 
    [SerializeField] private float turnSpeed = 100f;

    [Header("Boost Settings")]
    [SerializeField] private float boostMultiplier = 2f; // Double speed? Triple?

    private Rigidbody? rb;
    private bool isBoosting;
    private bool wasBoosting;
    private bool isBoostPending;
    private float boostWindupTimer;
    [SerializeField] private float boostWindupDelay = 1.0f;

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out rb))
        {
            Debug.LogError("SimpleDrive requires a Rigidbody component.");
            enabled = false;
            return;
        }
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // 1. DETECT BOOST INPUT (Visuals & Triggers go in Update)
        if (Keyboard.current != null)
        {
            wasBoosting = isBoosting;
            // Start windup if space is held and not already boosting or pending
            if (Keyboard.current.spaceKey.isPressed)
            {
                if (!isBoosting && !isBoostPending)
                {
                    isBoostPending = true;
                    boostWindupTimer = boostWindupDelay;
                }
            }
            else
            {
                isBoostPending = false;
                boostWindupTimer = 0f;
                isBoosting = false;
            }

            // Handle windup countdown
            if (isBoostPending)
            {
                boostWindupTimer -= Time.deltaTime;
                if (boostWindupTimer <= 0f)
                {
                    isBoostPending = false;
                    isBoosting = true;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float move = 0f;
        float turn = 0f;

        // 2. APPLY BOOST MULTIPLIERS
        // If boosting, use multiplied values. If not, use normal values.
        float currentMaxSpeed = isBoosting ? speed * boostMultiplier : speed;
        float currentAccel = isBoosting ? acceleration * boostMultiplier : acceleration;
        // Stop boost if space is released
        if (!isBoostPending && !isBoosting)
        {
            isBoosting = false;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move = 1f;
            if (Keyboard.current.sKey.isPressed) move = -1f;
            if (Keyboard.current.aKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed) turn = 1f;
        }

        // 3. MOVE (Physics)
        Vector3 velocity = rb.linearVelocity;
        Vector3 forward = transform.forward;
        float currentForwardSpeed = Vector3.Dot(velocity, forward);

        float turnAbs = Mathf.Abs(turn);
        float turnSpeedFactor = Mathf.Lerp(1f, 0.7f, turnAbs); 
        float turnAccelFactor = Mathf.Lerp(1f, 0.5f, turnAbs); 

        float effectiveTurn = (move == 0f) ? 0f : turn;

        if (move != 0f)
        {
            float targetSpeed = move * currentMaxSpeed * turnSpeedFactor;
            float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, currentAccel * turnAccelFactor * Time.fixedDeltaTime);
            velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
        }
        else
        {
            float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, deceleration * Time.fixedDeltaTime);
            velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
        }
        rb.linearVelocity = velocity;

        // 4. TURN
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / currentMaxSpeed); 
        
        float minTurnFactor = 0.35f; 
        float parabola = 1.5f * speedFactor * (1f - speedFactor); 
        float blend = Mathf.Clamp01((speedFactor - 0.15f) / 0.7f); 
        float turnFactor = Mathf.Lerp(minTurnFactor, parabola + minTurnFactor, blend);

        if (Mathf.Abs(effectiveTurn) > 0f)
        {
            float turnAmount = effectiveTurn * turnSpeed * turnFactor * Mathf.Deg2Rad;
            Vector3 angularVelocity = new(0f, turnAmount, 0f);
            rb.angularVelocity = angularVelocity;
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }
}