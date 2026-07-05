using UnityEngine;

namespace DriftAssignment.Vehicle
{
    public class SteeringAssist
    {
        private readonly CarConfig _config;
        private readonly WheelCollider _fl;
        private readonly WheelCollider _fr;
        private float _currentAngle;

        public SteeringAssist(WheelCollider fl, WheelCollider fr, CarConfig config)
        {
            _fl = fl;
            _fr = fr;
            _config = config;
        }

        public void Apply(float steerInput, float speedMps, float helperBlend, Vector3 localAngularVelocity)
        {
            var speedFactor = Mathf.Clamp01(speedMps / _config.SteerSpeedNormalization);
            var maxAngle = Mathf.Lerp(_config.MaxSteerAngle, _config.MinSteerAngleAtTopSpeed, speedFactor);
            var target = steerInput * maxAngle;

            if (helperBlend > 0f)
            {
                var counterSteer = -localAngularVelocity.y * helperBlend * 5f;
                target = Mathf.Lerp(target, counterSteer + target, helperBlend);
            }

            _currentAngle = Mathf.Lerp(_currentAngle, target, Time.deltaTime * _config.SteerLerpSpeed);
            _fl.steerAngle = _currentAngle;
            _fr.steerAngle = _currentAngle;
        }
    }
}
