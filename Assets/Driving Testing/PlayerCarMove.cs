using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using FishNet.Component.Prediction;
using System.Runtime.CompilerServices;

public class PlayerCarMove : NetworkBehaviour
{
    [Header("Wheel GameObjects")]
    [SerializeField] private List<GameObject> wheels = new List<GameObject>(); // FL, FR, RL, RR order

    [Header("Drive Settings")]
    [SerializeField] private float motorTorque = 1500f;
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] private float brakeTorque = 3000f;

    private float _moveInput;
    private float _turnInput;

    private void Update()
    {
        // other player filter
        //if (!IsOwner) return;

        // gets inputs
        _moveInput = Input.GetAxis("Vertical");
        _turnInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        // other player filter
        //if(!IsOwner) return;
        
        // for each of the wheels
        for(int i = 0; i < wheels.Count; i++) {
            // access collider of wheel
            var col = wheels[i].GetComponent<WheelCollider>();

            // spin at determined speed
            col.motorTorque = _moveInput * motorTorque;

            // if front two wheels
            if(i==0 || i == 1) {
                col.steerAngle = _turnInput * maxSteerAngle;
            }
            else {
                col.steerAngle = 0f;
            }

            // drift braking :O
            float brake = Input.GetKey(KeyCode.Space) ? brakeTorque : 0f;
            col.brakeTorque = brake;

            // get the wheel mesh to spin them, it assumes it is the first child object
            var mesh = wheels[i].transform.GetChild(0);
            UpdateWheelVisual(col, mesh);
        }
    }
    
    private void UpdateWheelVisual(WheelCollider col, Transform mesh) {
        Vector3 pos;

        // fancy rotation thing
        Quaternion rotation;

        // output positions
        col.GetWorldPose(out pos, out rotation);

        mesh.position = pos;
        mesh.rotation = rotation;
    }
}