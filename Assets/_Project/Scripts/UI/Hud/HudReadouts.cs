using DriftAssignment.Vehicle;
using TMPro;
using UnityEngine;

namespace DriftAssignment.UI.Hud
{
    /// Subscribes to CarController + TuningState events and mirrors speed /
    /// gear number / gear mode (AUTO or MANUAL) / RPM into TextMeshPro fields.
    [DisallowMultipleComponent]
    public class HudReadouts : MonoBehaviour
    {
        [SerializeField] private CarController _car;
        [SerializeField] private TuningState _tuning;
        [SerializeField] private TMP_Text _speedText;
        [SerializeField] private TMP_Text _gearText;
        [Tooltip("Small label next to the gear — swaps between AUTO / MANUAL based on TuningState.AutomaticTransmission")]
        [SerializeField] private TMP_Text _gearModeText;
        [SerializeField] private TMP_Text _rpmText;

        private void OnEnable()
        {
            if (_car != null)
            {
                _car.SpeedChanged += OnSpeed;
                _car.GearChanged += OnGear;
                _car.RpmChanged += OnRpm;
            }
            if (_tuning != null)
            {
                _tuning.Changed += OnTuningChanged;
                OnTuningChanged();
            }
        }

        private void OnDisable()
        {
            if (_car != null)
            {
                _car.SpeedChanged -= OnSpeed;
                _car.GearChanged -= OnGear;
                _car.RpmChanged -= OnRpm;
            }
            if (_tuning != null) _tuning.Changed -= OnTuningChanged;
        }

        private static readonly string[] _gearLabels =
            { "R", "N", "1", "2", "3", "4", "5", "6", "7", "8" };

        private int _lastSpeed = int.MinValue;
        private int _lastRpm = int.MinValue;

        private void OnTuningChanged()
        {
            if (_gearModeText != null)
            {
                _gearModeText.text = _tuning.AutomaticTransmission ? "AUTO" : "MANUAL";
            }
        }

        private void OnSpeed(float kmh)
        {
            if (_speedText == null) return;
            int rounded = Mathf.RoundToInt(kmh);
            if (rounded == _lastSpeed) return;
            _lastSpeed = rounded;
            _speedText.SetText("{0}", rounded);
        }

        private void OnGear(int gearIndex)
        {
            if (_gearText == null) return;
            int idx = Mathf.Clamp(gearIndex, 0, _gearLabels.Length - 1);
            _gearText.text = _gearLabels[idx];
        }

        private void OnRpm(float rpm)
        {
            if (_rpmText == null) return;
            int rounded = Mathf.RoundToInt(rpm);
            if (rounded == _lastRpm) return;
            _lastRpm = rounded;
            _rpmText.SetText("{0}", rounded);
        }
    }
}
