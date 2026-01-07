
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
    private float speed = 30f;

    [SerializeField]
    private float acceleration = 20f; // How quickly the car reaches top speed

        [SerializeField]
        private float deceleration = 10f; // Rolling resistance
    [SerializeField]
    private float turnSpeed = 100f;

    private Rigidbody? rb;

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out rb))
        {
            Debug.LogError("SimpleDrive requires a Rigidbody component.");
            enabled = false;
            return;
        }
        // SAFETY: If you forgot to freeze rotation in Inspector, we do it here.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float move = 0f;
        float turn = 0f;

        // 1. READ INPUT (Works with New Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move = 1f;
            if (Keyboard.current.sKey.isPressed) move = -1f;
            if (Keyboard.current.aKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed) turn = 1f;
        }

            // 2. MOVE (Physics-based with acceleration and gradual slowing)
            Vector3 velocity = rb.linearVelocity;
            Vector3 forward = transform.forward;
            float currentForwardSpeed = Vector3.Dot(velocity, forward);

            if (move != 0f)
            {
                // Accelerate toward target speed
                float targetSpeed = move * speed;
                float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
                velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
            }
            else
            {
                // Gradually slow down (rolling resistance)
                float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, deceleration * Time.fixedDeltaTime);
                velocity = forward * newForwardSpeed + transform.up * rb.linearVelocity.y;
            }
            rb.linearVelocity = velocity;

        // 3. TURN (Physics-based, slows with speed)
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / speed); // 0 when stopped, 1 at max speed
        if (speedFactor > 0.01f && Mathf.Abs(turn) > 0f)
        {
            float turnAmount = turn * turnSpeed * speedFactor * Mathf.Deg2Rad; // turn slows as speed drops
            Vector3 angularVelocity = new(0f, turnAmount, 0f);
            rb.angularVelocity = angularVelocity;
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }
}