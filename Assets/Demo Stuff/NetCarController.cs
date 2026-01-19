using FishNet.Object;
using FishNet.Object.Prediction; // CRITICAL FOR SMOOTH DRIVING
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;

// 1. The Input Data (What keys are we pressing?)
public struct MoveData : IReplicateData {
    public float Throttle;
    public float Turn;
    private uint _tick;

    // FishNet requirements
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

// 2. The Reconcile Data (Where is the car actually?)
public struct ReconcileData : IReconcileData {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    private uint _tick;

    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;

    // Constructor to easily capture state
    public ReconcileData(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel, uint tick) {
        Position = pos;
        Rotation = rot;
        Velocity = vel;
        AngularVelocity = angVel;
        _tick = tick;
    }
}

public class NetCarController : TickNetworkBehaviour {
    
    [Header("Car Settings")]
    public float acceleration = 20f;
    public float turnSpeed = 5f;
    public Rigidbody _rb;

    // A. Setup
    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    // B. Prediction Setup - Use SetTickCallbacks for Rigidbody prediction
    public override void OnStartNetwork() {
        base.OnStartNetwork();
        // Tell FishNet to run our physics logic during Tick and PostTick
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
    }

    // C. Gather Inputs and Run Prediction (Client Side)
    protected override void TimeManager_OnTick() {
        // Build and run replicate
        MoveData md = BuildMoveData();
        Move(md);
        
        // Create reconcile data
        CreateReconcile();
    }

    private MoveData BuildMoveData() {
        // Only the owner needs to build input data
        if (!base.IsOwner)
            return default;

        return new MoveData {
            Throttle = Input.GetAxis("Vertical"),
            Turn = Input.GetAxis("Horizontal")
        };
    }

    // D. The Actual Movement Logic (Runs on Client AND Server)
    [Replicate]
    private void Move(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        // Simple Physics Logic
        // Gas
        if (md.Throttle != 0) {
            _rb.AddForce(transform.forward * md.Throttle * acceleration, ForceMode.Acceleration);
        }
        
        // Steering (Only turn if moving, for realism)
        float speed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        if (Mathf.Abs(speed) > 1f) {
            float direction = speed > 0 ? 1 : -1; // Reverse steering when backing up
            float turnAmount = md.Turn * turnSpeed * direction * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            _rb.MoveRotation(_rb.rotation * turnRotation);
        }
    }

    // E. Server Corrects Client (Anti-Cheat / Sync)
    public override void CreateReconcile() {
        // Both server and client should create reconcile data
        ReconcileData rd = new ReconcileData(
            transform.position, 
            transform.rotation, 
            _rb.linearVelocity, 
            _rb.angularVelocity, 
            base.TimeManager.Tick
        );
        Reconcile(rd);
    }

    [Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable) {
        // Snap the car to the server's truth if it drifted too far
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _rb.linearVelocity = rd.Velocity;
        _rb.angularVelocity = rd.AngularVelocity;
    }
}