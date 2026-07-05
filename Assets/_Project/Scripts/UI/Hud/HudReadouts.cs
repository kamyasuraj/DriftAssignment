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

        private void OnTuningChanged()
        {
            if (_gearModeText != null)
            {
                _gearModeText.text = _tuning.AutomaticTransmission ? "AUTO" : "MANUAL";
            }
        }

        private void OnSpeed(float kmh)
        {
            if (_speedText != null) _speedText.text = $"{kmh:0}";
        }

        private void OnGear(int gearIndex)
        {
            if (_gearText == null) return;
            string label = gearIndex == 0 ? "R" : gearIndex == 1 ? "N" : (gearIndex - 1).ToString();
            _gearText.text = label;
        }

        private void OnRpm(float rpm)
        {
            if (_rpmText != null) _rpmText.text = $"{rpm:0}";
        }
    }
}
