using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

// 1. Move Data
public struct MoveData : IReplicateData {
    public float Throttle;
    public float Turn;
    public bool Jump;
    private uint _tick;

    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

// 2. Reconcile Data
public struct ReconcileData : IReconcileData {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    private uint _tick;

    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;

    public ReconcileData(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel, uint tick) {
        Position = pos;
        Rotation = rot;
        Velocity = vel;
        AngularVelocity = angVel;
        _tick = tick;
    }
}

// CHANGE: Inherit from NetworkBehaviour, not TickNetworkBehaviour
public class NetCarController : NetworkBehaviour {
    
    [Header("Car Settings")]
    public float acceleration = 50f;
    public float turnSpeed = 100f;
    public float jumpForce = 10f;
    public Rigidbody _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = new Vector3(0, -0.9f, 0);
    }

    // CHANGE: Subscribe to the Tick event manually
    public override void OnStartNetwork() {
        base.OnStartNetwork();
        // Listen to the server tick
        base.TimeManager.OnTick += TimeManager_OnTick;
    }

    // CHANGE: Unsubscribe when object is destroyed to prevent errors
    public override void OnStopNetwork() {
        base.OnStopNetwork();
        if (base.TimeManager != null)
            base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    // CHANGE: This is now a normal function we call, not an override
    private void TimeManager_OnTick() {
        MoveData md = BuildMoveData();
        Move(md);
        CreateReconcile();
    }

    private MoveData BuildMoveData() {
        if (!base.IsOwner)
            return default;

        return new MoveData {
            Throttle = Input.GetAxis("Vertical"),
            Turn = Input.GetAxis("Horizontal"),
            Jump = Input.GetKey(KeyCode.Space)
        };
    }

    [Replicate]
    private void Move(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        if (md.Throttle != 0) {
            _rb.AddForce(transform.forward * md.Throttle * acceleration, ForceMode.Acceleration);
        }

        float turnAmount = md.Turn * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        _rb.MoveRotation(_rb.rotation * turnRotation);

        if (md.Jump && transform.position.y < 2.0f) {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    public override void CreateReconcile() {
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
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _rb.linearVelocity = rd.Velocity;
        _rb.angularVelocity = rd.AngularVelocity;
    }
}