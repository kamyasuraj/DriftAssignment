using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [CreateAssetMenu(fileName = "CarConfig", menuName = "Drift/Car Config", order = 0)]
    public class CarConfig : ScriptableObject
    {
        [Header("Mass & Balance")]
        public float Mass = 1500f;
        public Vector3 CenterOfMassOffset = new Vector3(0f, -0.4f, 0.1f);
        public float LinearDrag = 0.05f;
        public float AngularDrag = 0.15f;

        [Header("Engine")]
        public float MaxMotorTorque = 2200f;
        public float MaxBrakeTorque = 3800f;
        public float MaxHandBrakeTorque = 5500f;
        public float MinRpm = 900f;
        public float MaxRpm = 7500f;
        public float RpmSmoothing = 8f;

        [Header("Gearbox")]
        public DrivetrainMode DefaultDrivetrain = DrivetrainMode.Rwd;
        public float[] GearRatios = { -3.2f, 0f, 3.6f, 2.2f, 1.55f, 1.2f, 0.95f, 0.78f };
        public float FinalDriveRatio = 3.5f;
        public float UpshiftRpm = 6800f;
        public float DownshiftRpm = 2600f;

        [Header("Steering")]
        public float MaxSteerAngle = 32f;
        public float MinSteerAngleAtTopSpeed = 12f;
        public float SteerSpeedNormalization = 40f;
        public float SteerLerpSpeed = 8f;

        [Header("Wheel — Forward Friction")]
        public float ForwardExtremumSlip = 0.4f;
        public float ForwardExtremumValue = 1f;
        public float ForwardAsymptoteSlip = 0.8f;
        public float ForwardAsymptoteValue = 0.7f;
        public float ForwardStiffness = 1.8f;

        [Header("Wheel — Sideways Friction")]
        public float SidewaysExtremumSlip = 0.25f;
        public float SidewaysExtremumValue = 1f;
        public float SidewaysAsymptoteSlip = 0.55f;
        public float SidewaysAsymptoteValue = 0.7f;
        public float SidewaysStiffness = 1.5f;
        public float SidewaysStiffnessOnHandBrake = 0.55f;
    }
}
