using UnityEngine;


public class ArcadeCar : MonoBehaviour
{
    public Rigidbody rb;
    public float speed = 50f;
    public float turnSpeed = 100f;
    public float hoverHeight = 1.0f; // How high the car floats visually
    public Transform[] wheelVisuals; // Drag your 4 wheel meshes here

    void Start()
    {
        // Prevent car from tipping over
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void FixedUpdate()
    {
        // 1. Fake Gravity / Hover (Keeps car upright and floating)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, hoverHeight + 1f))
        {
            float distance = hit.distance;
            float force = (hoverHeight - distance) * rb.mass * 20f; // Spring force
            rb.AddForce(transform.up * force);
        }

        // 2. Move Forward & Turn (New Input System)
        float gas = 0f;
        float turn = 0f;

        // Keyboard Support
        if (UnityEngine.InputSystem.Keyboard.current != null) {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) gas = 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) gas = -1f;
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) turn = -1f;
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) turn = 1f;
        }

        // Gamepad Support
        if (UnityEngine.InputSystem.Gamepad.current != null) {
            Vector2 stick = UnityEngine.InputSystem.Gamepad.current.leftStick.ReadValue();
            if (stick.y != 0) gas = stick.y;
            if (stick.x != 0) turn = stick.x;
        }

        // Move along Z axis (forward/back)
        rb.AddForce(transform.forward * gas * speed * 10f);
        // Rotate around Z axis (roll)
        Quaternion turnRot = Quaternion.AngleAxis(turn * turnSpeed * Time.fixedDeltaTime, transform.forward);
        rb.MoveRotation(rb.rotation * turnRot);

        // 4. Visual: Rotate only the mesh (first child) of each wheel
        foreach(var w in wheelVisuals)
        {
            if (w.childCount > 0)
            {
                Transform mesh = w.GetChild(0);
                mesh.Rotate(mesh.right * gas * 10f);
            }
        }
    }
}