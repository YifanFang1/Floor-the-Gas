using UnityEngine;

public class CarAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform visualModel;   
    public Transform[] wheels;      
    
    [Header("Settings")]
    public float stretchFactor = 0.01f; 
    public float wheelSpinSpeed = 10f;
    
    private Rigidbody rb;
    private float currentSquash = 1f;
    
    // NEW: Variable to store your original size
    private Vector3 initialScale; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // MEMORIZE: "How big did the user make the car in the Inspector?"
        if (visualModel)
        {
            initialScale = visualModel.localScale;
        }
    }

    void Update()
    {
        if (rb == null || visualModel == null) return;
        
        // Unity 6 check (use .velocity if .linearVelocity errors)
        float speed = rb.linearVelocity.magnitude;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // 1. SQUASH & STRETCH (Relative to Initial Scale)
        float targetStretch = 1f + (speed * stretchFactor);
        currentSquash = Mathf.Lerp(currentSquash, targetStretch, Time.deltaTime * 5f);
        
        float stretch = currentSquash;
        float squash = 1f / stretch;
        
        // Apply the stretch math to the ORIGINAL size
        Vector3 newScale = new Vector3(squash, squash, stretch);
        
        // Vector3.Scale multiplies them together (e.g., 100 * 1 = 100)
        visualModel.localScale = Vector3.Scale(initialScale, newScale);

        // 2. WHEEL SPIN
        foreach (Transform w in wheels)
        {
            if (w) 
            {
                w.Rotate(forwardSpeed * wheelSpinSpeed * Time.deltaTime, 0, 0);
            }
        }
    }
}