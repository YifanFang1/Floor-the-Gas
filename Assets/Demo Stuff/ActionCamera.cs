using UnityEngine;

public class ActionCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // Drag your "Car_Player" here

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.2f, -4.2f); // Closer to car
    public float smoothSpeed = 8f;    // Smoother, faster follow
    public float rotationSpeed = 10f;

    [Header("Speed FX")]
    public float baseFOV = 60f;
    public float maxFOV = 90f;

    [Header("Tilt")]
    public float tiltAngle = 10f; // Degrees to tilt up

    private Rigidbody targetRb;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target) targetRb = target.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1. POSITION: Smooth follow
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // 2. ROTATION: Look at the car, but dampen it, then tilt up
        Vector3 direction = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        // Apply tilt (rotate around local X axis)
        rotation *= Quaternion.Euler(-tiltAngle, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        // 3. FOV WARP: The "Redline" Effect
        if (targetRb && cam)
        {
            float speed = targetRb.linearVelocity.magnitude;
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speed / 50f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
        }
    }
}
