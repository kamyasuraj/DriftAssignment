using System;
using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class CarController : MonoBehaviour
    {
        [Header("Config & Tuning")]
        [SerializeField] private CarConfig _config;
        [SerializeField] private TuningState _tuning;

        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider _wheelFL;
        [SerializeField] private WheelCollider _wheelFR;
        [SerializeField] private WheelCollider _wheelRL;
        [SerializeField] private WheelCollider _wheelRR;

        [Header("Input Source (implements IInputProvider)")]
        [SerializeField] private MonoBehaviour _inputSource;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        private Rigidbody _rigidbody;
        private IInputProvider _input;
        private Drivetrain _drivetrain;
        private GearBox _gearBox;
        private HandBrake _handBrake;
        private SteeringAssist _steering;

        private float _lastSpeedKmh;

        public event Action<float> RpmChanged;
        public event Action<int> GearChanged;
        public event Action<float> SpeedChanged;

        public float EngineRpm => _gearBox?.EngineRpm ?? 0f;
        public int CurrentGear => _gearBox?.CurrentGear ?? 1;
        public float SpeedKmh => _rigidbody != null ? _rigidbody.linearVelocity.magnitude * 3.6f : 0f;

        // Latest input snapshot — updated each FixedUpdate. Read-only for other systems (CarAudio).
        public float ThrottleInput { get; private set; }
        public float BrakeInput { get; private set; }
        public float SteerInput { get; private set; }
        public bool HandBrakeActive { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_config == null)
            {
                Debug.LogError("[CarController] CarConfig is not assigned.", this);
                enabled = false;
                return;
            }

            _rigidbody.mass = _config.Mass;
            _rigidbody.centerOfMass = _config.CenterOfMassOffset;
            _rigidbody.linearDamping = _config.LinearDrag;
            _rigidbody.angularDamping = _config.AngularDrag;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _input = _inputSource as IInputProvider;
            if (_input == null)
            {
                Debug.LogError("[CarController] Assigned input source does not implement IInputProvider.", this);
                enabled = false;
                return;
            }

            _drivetrain = new Drivetrain(_wheelFL, _wheelFR, _wheelRL, _wheelRR);
            _gearBox = new GearBox(_config);
            _handBrake = new HandBrake(_wheelRL, _wheelRR, _config);
            _steering = new SteeringAssist(_wheelFL, _wheelFR, _config);

            _gearBox.GearChanged += OnGearChanged;
            _gearBox.RpmChanged += r => RpmChanged?.Invoke(r);

            ApplyFrictionCurves();

            if (_tuning != null) _tuning.Changed += OnTuningChanged;

            if (_debugLog)
            {
                Debug.Log($"[CarController] Awake. Drivetrain={_tuning?.Drivetrain ?? _config.DefaultDrivetrain} " +
                          $"Transmission={(_tuning?.AutomaticTransmission ?? true ? "Auto" : "Manual")} " +
                          $"Mass={_config.Mass}kg CoM={_config.CenterOfMassOffset} MaxTorque={_config.MaxMotorTorque}", this);
            }
        }

        private void OnGearChanged(int gear)
        {
            GearChanged?.Invoke(gear);
            if (_debugLog)
            {
                var label = gear == 0 ? "R" : gear == 1 ? "N" : (gear - 1).ToString();
                Debug.Log($"[CarController] Gear → {label} (index {gear})", this);
            }
        }

        private void OnDestroy()
        {
            if (_tuning != null) _tuning.Changed -= OnTuningChanged;
        }

        private void FixedUpdate()
        {
            var mode = _tuning != null ? _tuning.Drivetrain : _config.DefaultDrivetrain;
            var automatic = _tuning == null || _tuning.AutomaticTransmission;

            var throttle = _input.Throttle;
            var brake = _input.Brake;
            var steer = Mathf.Clamp(_input.Steer, -1f, 1f);

            ThrottleInput = throttle;
            BrakeInput = brake;
            SteerInput = steer;
            HandBrakeActive = _input.HandBrake;

            var localVel = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            var forwardVel = localVel.z;
            var goingForward = forwardVel > 0.5f;
            var goingReverse = forwardVel < -0.5f;
            var inReverse = _gearBox.CurrentGear == 0;

            // Automatic reverse — brake at low speed while going forward or stopped → shift to reverse
            if (automatic)
            {
                if (brake > 0.1f && throttle < 0.1f && !goingForward && !inReverse)
                {
                    _gearBox.SetReverse();
                    inReverse = true;
                    if (_debugLog) Debug.Log("[CarController] Auto → Reverse (brake at low speed)", this);
                }
                else if (throttle > 0.1f && inReverse && !goingReverse)
                {
                    _gearBox.SetForwardFirst();
                    inReverse = false;
                    if (_debugLog) Debug.Log("[CarController] Auto → 1st (throttle from stopped reverse)", this);
                }
            }

            // In reverse, S = motor drive (backwards), W = brake to stop.
            float motorInput, brakeInput;
            if (inReverse) { motorInput = brake; brakeInput = throttle; }
            else           { motorInput = throttle; brakeInput = brake; }

            var drivenRpm = _drivetrain.AverageDrivenWheelRpm(mode);
            _gearBox.Update(drivenRpm, motorInput, automatic, _input.ShiftUp, _input.ShiftDown);

            var gearRatio = _gearBox.GetGearRatio();
            var motorSign = Mathf.Sign(gearRatio);
            var motorTorque = motorInput * _config.MaxMotorTorque * motorSign;

            _drivetrain.ApplyBrake(brakeInput * _config.MaxBrakeTorque);
            _drivetrain.ApplyTorque(motorTorque, mode);
            _handBrake.Apply(_input.HandBrake);

            var speedMps = _rigidbody.linearVelocity.magnitude;
            var helper = _tuning != null ? _tuning.HelperValue : 0.35f;
            var localAngular = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            _steering.Apply(steer, speedMps, helper, localAngular);

            var kmh = speedMps * 3.6f;
            if (!Mathf.Approximately(kmh, _lastSpeedKmh))
            {
                _lastSpeedKmh = kmh;
                SpeedChanged?.Invoke(kmh);
            }
        }

        private void OnTuningChanged()
        {
            ApplyFrictionCurves();
            ApplySuspension();
        }

        private void ApplyFrictionCurves()
        {
            SetForwardFriction(_wheelFL);
            SetForwardFriction(_wheelFR);
            SetForwardFriction(_wheelRL);
            SetForwardFriction(_wheelRR);
            SetSidewaysFriction(_wheelFL);
            SetSidewaysFriction(_wheelFR);
            SetSidewaysFriction(_wheelRL);
            SetSidewaysFriction(_wheelRR);
        }

        private void SetForwardFriction(WheelCollider wheel)
        {
            var f = wheel.forwardFriction;
            f.extremumSlip = _config.ForwardExtremumSlip;
            f.extremumValue = _config.ForwardExtremumValue;
            f.asymptoteSlip = _config.ForwardAsymptoteSlip;
            f.asymptoteValue = _config.ForwardAsymptoteValue;
            f.stiffness = _config.ForwardStiffness;
            wheel.forwardFriction = f;
        }

        private void SetSidewaysFriction(WheelCollider wheel)
        {
            var f = wheel.sidewaysFriction;
            f.extremumSlip = _config.SidewaysExtremumSlip;
            f.extremumValue = _config.SidewaysExtremumValue;
            f.asymptoteSlip = _config.SidewaysAsymptoteSlip;
            f.asymptoteValue = _config.SidewaysAsymptoteValue;
            f.stiffness = _config.SidewaysStiffness;
            wheel.sidewaysFriction = f;
        }

        private void ApplySuspension()
        {
            if (_tuning == null) return;
            ApplySpringForce(_wheelFL, _tuning.FrontSpringForce);
            ApplySpringForce(_wheelFR, _tuning.FrontSpringForce);
            ApplySpringForce(_wheelRL, _tuning.RearSpringForce);
            ApplySpringForce(_wheelRR, _tuning.RearSpringForce);
        }

        private static void ApplySpringForce(WheelCollider wheel, float springForce)
        {
            var s = wheel.suspensionSpring;
            s.spring = springForce;
            wheel.suspensionSpring = s;
        }
    }
}
