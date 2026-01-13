#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple physics-based driving controller for Unity.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleDrive : MonoBehaviour
{
    [SerializeField]
    private float speed = 50f; // Increased top speed

    [SerializeField]
    private float acceleration = 8f; // Decreased acceleration for slower speed-up

        [SerializeField]
        private float deceleration = 10f; // Rolling resistance
    [SerializeField]
    private float turnSpeed = 160f; // Sharper turning

    private Rigidbody? rb;

    [Header("Physics Options")]
    [Tooltip("Allow the car to rotate freely (unfreeze X/Z rotation)?")]
    public bool allowRotation = false;

    [Header("Boost Settings")]
    [SerializeField]
    private float boostSpeed = 120f;
    [SerializeField]
    private float boostAcceleration = 40f;
    [SerializeField]
    private float boostTurnFactor = 0.15f; // Minimal turning during boost
    [SerializeField]
    private float boostDeceleration = 12f;

    private bool isBoosting = false;
    private float boostTimer = 0f;
    private bool isBoostPending = false;
    private float boostDelayTimer = 0f;
    [SerializeField]
    private float boostDelay = 0.5f; // Delay before boost starts
    [SerializeField]
    private float boostDuration = 0.7f; // Duration of boost effect in seconds
    [SerializeField]
    private float boostInitialMultiplier = 2.5f; // How much faster at the start of boost

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out rb))
        {
            Debug.LogError("SimpleDrive requires a Rigidbody component.");
            enabled = false;
            return;
        }
        // SAFETY: If you forgot to freeze rotation in Inspector, we do it here.
        if (allowRotation)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;


        float move = 0f;
        float turn = 0f;
        // Only set isBoosting to false if not currently boosting
        if (boostTimer <= 0f)
            isBoosting = false;

        // 1. READ INPUT (Works with New Input System)

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move = 1f;
            if (Keyboard.current.sKey.isPressed) move = -1f;
            if (Keyboard.current.aKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed) turn = 1f;
            if (Keyboard.current.spaceKey.isPressed)
            {
                // Only allow boost when moving forward and not already boosting or pending
                if (move > 0f && boostTimer <= 0f && !isBoostPending)
                {
                    isBoostPending = true;
                    boostDelayTimer = boostDelay;
                }
            }
        }


        // 2. MOVE (Physics-based with acceleration and gradual slowing)
        Vector3 velocity = rb.linearVelocity;
        Vector3 forward = transform.forward;
        float currentForwardSpeed = Vector3.Dot(velocity, forward);

        float turnAbs = Mathf.Abs(turn);
        float turnSpeedFactor = Mathf.Lerp(1f, 0.7f, turnAbs); // 1 when straight, 0.7 when full turn
        float turnAccelFactor = Mathf.Lerp(1f, 0.85f, turnAbs); // 1 when straight, 0.85 when full turn (less reduction)

        // Allow turning if the car has any speed, not just when move input is pressed
        float minSpeedForTurn = 0.5f;
        float effectiveTurn = (Mathf.Abs(currentForwardSpeed) > minSpeedForTurn) ? turn : 0f;

        // Handle boost delay
        if (isBoostPending)
        {
            boostDelayTimer -= Time.fixedDeltaTime;
            if (boostDelayTimer <= 0f)
            {
                isBoostPending = false;
                isBoosting = true;
                boostTimer = boostDuration;
            }
        }

        if (boostTimer > 0f)
        {
            // Decaying boost: strong initial jump, then fades
            float t = 1f - (boostTimer / boostDuration); // 0 at start, 1 at end
            float decay = Mathf.Lerp(boostInitialMultiplier, 1f, t); // Exponential/linear decay
            float targetSpeed = boostSpeed * decay;
            float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, boostAcceleration * decay * Time.fixedDeltaTime);
            velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
            rb.linearVelocity = velocity;
            boostTimer -= Time.fixedDeltaTime;
            if (boostTimer <= 0f)
            {
                boostTimer = 0f;
                isBoosting = false;
            }
        }
        else if (move != 0f)
        {
            // If holding back while moving forward, apply strong deceleration
            if (move < 0f && currentForwardSpeed > 1f)
            {
                // Fast slowdown, not instant reversal
                float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, (deceleration * 4f) * Time.fixedDeltaTime);
                velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
                rb.linearVelocity = velocity;
            }
            else
            {
                // Normal acceleration
                float targetSpeed = move * speed * turnSpeedFactor;
                float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, acceleration * turnAccelFactor * Time.fixedDeltaTime);
                velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
                rb.linearVelocity = velocity;
            }
        }
        else
        {
            // Gradually slow down (rolling resistance)
            float decel = isBoosting ? boostDeceleration : deceleration;
            float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, decel * Time.fixedDeltaTime);
            velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }

        // 3. TURN (Physics-based, slows with speed)
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedForTurn = isBoosting ? boostSpeed : speed;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / speedForTurn); // 0 when stopped, 1 at max speed
        float minTurnFactor = isBoosting ? boostTurnFactor : 0.5f; // Allow sharper minimum turning
        float parabola = 1.5f * speedFactor * (1f - speedFactor);
        float blend = Mathf.Clamp01((speedFactor - 0.15f) / 0.7f);
        float turnFactor = isBoosting ? boostTurnFactor : Mathf.Lerp(minTurnFactor, parabola + minTurnFactor, blend);
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