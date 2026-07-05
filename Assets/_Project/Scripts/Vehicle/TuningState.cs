using System;
using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [CreateAssetMenu(fileName = "TuningState", menuName = "Drift/Tuning State", order = 1)]
    public class TuningState : ScriptableObject
    {
        [Header("Drivetrain")]
        public DrivetrainMode Drivetrain = DrivetrainMode.Rwd;
        public bool AutomaticTransmission = true;

        [Header("Suspension Spring Force")]
        [Range(500f, 6000f)] public float FrontSpringForce = 1200f;
        [Range(500f, 6000f)] public float RearSpringForce = 1000f;

        [Header("Ride Height")]
        [Range(-0.15f, 0.25f)] public float FrontRideHeight = 0f;
        [Range(-0.15f, 0.25f)] public float RearRideHeight = 0f;

        [Header("Camber")]
        [Range(-6f, 6f)] public float FrontCamber = 0f;
        [Range(-6f, 6f)] public float RearCamber = 0f;

        [Header("Assist")]
        [Range(0f, 1f)] public float HelperValue = 0.35f;

        [Header("Hydraulics (stretch)")]
        public bool Hydraulics = false;

        public event Action Changed;

        public void RaiseChanged() => Changed?.Invoke();

        private void OnValidate() => Changed?.Invoke();
    }
}
