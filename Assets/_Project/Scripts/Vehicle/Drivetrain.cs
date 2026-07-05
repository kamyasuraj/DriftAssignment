using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    public class Drivetrain
    {
        private readonly WheelCollider _fl;
        private readonly WheelCollider _fr;
        private readonly WheelCollider _rl;
        private readonly WheelCollider _rr;

        public Drivetrain(WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr)
        {
            _fl = fl;
            _fr = fr;
            _rl = rl;
            _rr = rr;
        }

        public void ApplyTorque(float torque, DrivetrainMode mode)
        {
            switch (mode)
            {
                case DrivetrainMode.Fwd:
                    _fl.motorTorque = torque * 0.5f;
                    _fr.motorTorque = torque * 0.5f;
                    _rl.motorTorque = 0f;
                    _rr.motorTorque = 0f;
                    break;

                case DrivetrainMode.Rwd:
                    _fl.motorTorque = 0f;
                    _fr.motorTorque = 0f;
                    _rl.motorTorque = torque * 0.5f;
                    _rr.motorTorque = torque * 0.5f;
                    break;

                case DrivetrainMode.Awd:
                    _fl.motorTorque = torque * 0.3f;
                    _fr.motorTorque = torque * 0.3f;
                    _rl.motorTorque = torque * 0.2f;
                    _rr.motorTorque = torque * 0.2f;
                    break;
            }
        }

        public void ApplyBrake(float brake)
        {
            _fl.brakeTorque = brake;
            _fr.brakeTorque = brake;
            _rl.brakeTorque = brake;
            _rr.brakeTorque = brake;
        }

        public float AverageDrivenWheelRpm(DrivetrainMode mode)
        {
            switch (mode)
            {
                case DrivetrainMode.Fwd: return (Mathf.Abs(_fl.rpm) + Mathf.Abs(_fr.rpm)) * 0.5f;
                case DrivetrainMode.Rwd: return (Mathf.Abs(_rl.rpm) + Mathf.Abs(_rr.rpm)) * 0.5f;
                default: return (Mathf.Abs(_fl.rpm) + Mathf.Abs(_fr.rpm) + Mathf.Abs(_rl.rpm) + Mathf.Abs(_rr.rpm)) * 0.25f;
            }
        }
    }
}
