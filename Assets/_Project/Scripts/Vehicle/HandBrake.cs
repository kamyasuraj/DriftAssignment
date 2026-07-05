using UnityEngine;

namespace DriftAssignment.Vehicle
{
    public class HandBrake
    {
        private readonly WheelCollider _rl;
        private readonly WheelCollider _rr;
        private readonly float _handBrakeTorque;
        private readonly float _driftSidewaysStiffness;
        private readonly float _defaultSidewaysStiffness;

        public HandBrake(WheelCollider rl, WheelCollider rr, CarConfig config)
        {
            _rl = rl;
            _rr = rr;
            _handBrakeTorque = config.MaxHandBrakeTorque;
            _driftSidewaysStiffness = config.SidewaysStiffnessOnHandBrake;
            _defaultSidewaysStiffness = config.SidewaysStiffness;
        }

        public void Apply(bool active)
        {
            var torque = active ? _handBrakeTorque : 0f;
            _rl.brakeTorque = Mathf.Max(_rl.brakeTorque, torque);
            _rr.brakeTorque = Mathf.Max(_rr.brakeTorque, torque);

            var target = active ? _driftSidewaysStiffness : _defaultSidewaysStiffness;
            SetSidewaysStiffness(_rl, target);
            SetSidewaysStiffness(_rr, target);
        }

        private static void SetSidewaysStiffness(WheelCollider wheel, float stiffness)
        {
            var f = wheel.sidewaysFriction;
            f.stiffness = stiffness;
            wheel.sidewaysFriction = f;
        }
    }
}
