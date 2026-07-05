using System;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    public class GearBox
    {
        private readonly CarConfig _config;

        public int CurrentGear { get; private set; } = 1;
        public float EngineRpm { get; private set; }

        public event Action<int> GearChanged;
        public event Action<float> RpmChanged;

        public GearBox(CarConfig config)
        {
            _config = config;
        }

        public float GetGearRatio() => _config.GearRatios[CurrentGear] * _config.FinalDriveRatio;

        public void Update(float drivenWheelRpm, float throttle, bool automatic, bool shiftUpRequested, bool shiftDownRequested)
        {
            var ratio = Mathf.Abs(GetGearRatio());
            var targetRpm = Mathf.Max(_config.MinRpm, drivenWheelRpm * ratio);
            var smoothed = Mathf.Lerp(EngineRpm, targetRpm, Time.deltaTime * _config.RpmSmoothing);
            if (!Mathf.Approximately(EngineRpm, smoothed))
            {
                EngineRpm = smoothed;
                RpmChanged?.Invoke(EngineRpm);
            }

            // Skip auto-shift logic when in reverse — CarController owns reverse/forward transitions
            if (CurrentGear == 0) return;

            if (automatic)
            {
                if (EngineRpm > _config.UpshiftRpm && throttle > 0.2f) TryShift(+1);
                else if (EngineRpm < _config.DownshiftRpm && CurrentGear > 2) TryShift(-1);
            }
            else
            {
                if (shiftUpRequested) TryShift(+1);
                if (shiftDownRequested) TryShift(-1);
            }
        }

        public void SetReverse()
        {
            if (CurrentGear != 0)
            {
                CurrentGear = 0;
                GearChanged?.Invoke(CurrentGear);
            }
        }

        public void SetNeutral()
        {
            if (CurrentGear != 1)
            {
                CurrentGear = 1;
                GearChanged?.Invoke(CurrentGear);
            }
        }

        public void SetForwardFirst()
        {
            if (CurrentGear != 2)
            {
                CurrentGear = 2;
                GearChanged?.Invoke(CurrentGear);
            }
        }

        private void TryShift(int delta)
        {
            var next = Mathf.Clamp(CurrentGear + delta, 0, _config.GearRatios.Length - 1);
            if (next == CurrentGear) return;
            CurrentGear = next;
            GearChanged?.Invoke(CurrentGear);
        }
    }
}
