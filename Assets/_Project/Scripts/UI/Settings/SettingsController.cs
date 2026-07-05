using DriftAssignment.Core;
using DriftAssignment.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DriftAssignment.UI.Settings
{
    /// Wires all Settings-panel controls to their backing data (TuningState,
    /// AudioListener volume, Application.targetFrameRate). Two-way binding:
    /// UI change → data write; TuningState.Changed → UI refresh.
    ///
    /// Explicit fields for each control keep the wiring inspectable and
    /// type-safe (vs. reflection-driven bindings which are opaque).
    [DisallowMultipleComponent]
    public class SettingsController : MonoBehaviour
    {
        [Header("Backing data")]
        [SerializeField] private TuningState _tuning;

        [Header("Transmission tab")]
        [SerializeField] private Toggle _autoTransmissionToggle;
        [SerializeField] private TMP_Text _autoTransmissionLabel;
        [SerializeField] private TMP_Dropdown _drivetrainDropdown;
        [SerializeField] private Slider _helperSlider;
        [SerializeField] private TMP_Text _helperValueText;

        [Header("Suspension tab")]
        [SerializeField] private Slider _frontSpringSlider;
        [SerializeField] private TMP_Text _frontSpringValueText;
        [SerializeField] private Slider _rearSpringSlider;
        [SerializeField] private TMP_Text _rearSpringValueText;
        [SerializeField] private Slider _frontRideHeightSlider;
        [SerializeField] private TMP_Text _frontRideHeightValueText;
        [SerializeField] private Slider _rearRideHeightSlider;
        [SerializeField] private TMP_Text _rearRideHeightValueText;

        [Header("Camber tab")]
        [SerializeField] private Slider _frontCamberSlider;
        [SerializeField] private TMP_Text _frontCamberValueText;
        [SerializeField] private Slider _rearCamberSlider;
        [SerializeField] private TMP_Text _rearCamberValueText;

        [Header("Audio tab")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private TMP_Text _masterVolumeValueText;

        [Header("Graphics tab")]
        [SerializeField] private TMP_Dropdown _fpsCapDropdown;
        [SerializeField] private TMP_Dropdown _graphicQualityDropdown;
        [SerializeField] private TMP_Dropdown _antiAliasingDropdown;
        [SerializeField] private Toggle _postProcessingToggle;
        [SerializeField] private TMP_Text _postProcessingLabel;
        [SerializeField] private TMP_Dropdown _shadowQualityDropdown;

        [Header("Footer")]
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private SettingsPanel _panel;

        // Snapshot of TuningState at Awake — restored by RESET
        private TuningDefaults _defaults;

        private struct TuningDefaults
        {
            public DrivetrainMode Drivetrain;
            public bool AutomaticTransmission;
            public float FrontSpringForce, RearSpringForce;
            public float FrontRideHeight, RearRideHeight;
            public float FrontCamber, RearCamber;
            public float HelperValue;
            public float MasterVolume;
        }

        private void Awake()
        {
            SnapshotDefaults();
            PopulateDropdowns();
            WireEvents();
            RefreshFromData();
            if (_tuning != null) _tuning.Changed += RefreshFromData;
        }

        private void OnDestroy()
        {
            if (_tuning != null) _tuning.Changed -= RefreshFromData;
        }

        // ---- Wiring ----
        private void WireEvents()
        {
            if (_autoTransmissionToggle != null)
                _autoTransmissionToggle.onValueChanged.AddListener(v =>
                {
                    if (_tuning == null) return;
                    _tuning.AutomaticTransmission = v;
                    _tuning.RaiseChanged();
                });

            if (_drivetrainDropdown != null)
                _drivetrainDropdown.onValueChanged.AddListener(v =>
                {
                    if (_tuning == null) return;
                    _tuning.Drivetrain = (DrivetrainMode)v;
                    _tuning.RaiseChanged();
                });

            if (_helperSlider != null)
                _helperSlider.onValueChanged.AddListener(v => SetTuning(t => t.HelperValue = v));

            if (_frontSpringSlider != null)
                _frontSpringSlider.onValueChanged.AddListener(v => SetTuning(t => t.FrontSpringForce = v));
            if (_rearSpringSlider != null)
                _rearSpringSlider.onValueChanged.AddListener(v => SetTuning(t => t.RearSpringForce = v));
            if (_frontRideHeightSlider != null)
                _frontRideHeightSlider.onValueChanged.AddListener(v => SetTuning(t => t.FrontRideHeight = v));
            if (_rearRideHeightSlider != null)
                _rearRideHeightSlider.onValueChanged.AddListener(v => SetTuning(t => t.RearRideHeight = v));

            if (_frontCamberSlider != null)
                _frontCamberSlider.onValueChanged.AddListener(v => SetTuning(t => t.FrontCamber = v));
            if (_rearCamberSlider != null)
                _rearCamberSlider.onValueChanged.AddListener(v => SetTuning(t => t.RearCamber = v));

            if (_masterVolumeSlider != null)
                _masterVolumeSlider.onValueChanged.AddListener(v =>
                {
                    AudioListener.volume = v;
                    if (_masterVolumeValueText != null) _masterVolumeValueText.text = $"{v * 100f:0}%";
                });

            if (_fpsCapDropdown != null)
                _fpsCapDropdown.onValueChanged.AddListener(v =>
                {
                    // 0=30, 1=60, 2=Uncapped
                    Application.targetFrameRate = v == 0 ? 30 : v == 1 ? 60 : -1;
                });
            if (_graphicQualityDropdown != null)
                _graphicQualityDropdown.onValueChanged.AddListener(v => QualitySettings.SetQualityLevel(v, true));
            if (_antiAliasingDropdown != null)
                _antiAliasingDropdown.onValueChanged.AddListener(SetAntiAliasing);
            if (_postProcessingToggle != null)
                _postProcessingToggle.onValueChanged.AddListener(v =>
                {
                    SetPostProcessing(v);
                    if (_postProcessingLabel != null) _postProcessingLabel.text = v ? "ON" : "OFF";
                });
            if (_shadowQualityDropdown != null)
                _shadowQualityDropdown.onValueChanged.AddListener(SetShadowQuality);

            if (_resetButton != null) _resetButton.onClick.AddListener(ResetToDefaults);
            if (_restartButton != null) _restartButton.onClick.AddListener(RestartScene);
            if (_continueButton != null) _continueButton.onClick.AddListener(() => { if (_panel != null) _panel.Close(); });
        }

        private void SetTuning(System.Action<TuningState> setter)
        {
            if (_tuning == null) return;
            setter(_tuning);
            _tuning.RaiseChanged();
        }

        // ---- Population ----
        private void PopulateDropdowns()
        {
            if (_drivetrainDropdown != null)
            {
                _drivetrainDropdown.ClearOptions();
                _drivetrainDropdown.AddOptions(new System.Collections.Generic.List<string> { "FWD", "RWD", "AWD" });
            }
            if (_fpsCapDropdown != null)
            {
                _fpsCapDropdown.ClearOptions();
                _fpsCapDropdown.AddOptions(new System.Collections.Generic.List<string> { "30 FPS", "60 FPS", "UNCAPPED" });
            }
            if (_graphicQualityDropdown != null)
            {
                _graphicQualityDropdown.ClearOptions();
                _graphicQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "LOW", "MEDIUM", "HIGH" });
            }
            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.ClearOptions();
                _antiAliasingDropdown.AddOptions(new System.Collections.Generic.List<string> { "OFF", "FXAA", "SMAA" });
            }
            if (_shadowQualityDropdown != null)
            {
                _shadowQualityDropdown.ClearOptions();
                _shadowQualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "OFF", "LOW", "HIGH" });
            }
        }

        // ---- Graphics helpers ----
        private static void SetAntiAliasing(int mode)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null) return;
            data.antialiasing = mode switch
            {
                1 => UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing,
                2 => UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                _ => UnityEngine.Rendering.Universal.AntialiasingMode.None,
            };
        }

        private static void SetPostProcessing(bool on)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = on;
        }

        private static void SetShadowQuality(int mode)
        {
            // 0=Off, 1=Low, 2=High — flips QualitySettings.shadows + distance
            switch (mode)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 25f;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 80f;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
            }
        }

        // ---- UI refresh ----
        private void RefreshFromData()
        {
            if (_tuning == null) return;
            SetToggleSilent(_autoTransmissionToggle, _tuning.AutomaticTransmission);
            if (_autoTransmissionLabel != null) _autoTransmissionLabel.text = _tuning.AutomaticTransmission ? "AUTO" : "MANUAL";
            SetDropdownSilent(_drivetrainDropdown, (int)_tuning.Drivetrain);

            SetSliderSilent(_helperSlider, _tuning.HelperValue);
            if (_helperValueText != null) _helperValueText.text = $"{_tuning.HelperValue:0.00}";

            SetSliderSilent(_frontSpringSlider, _tuning.FrontSpringForce);
            if (_frontSpringValueText != null) _frontSpringValueText.text = $"{_tuning.FrontSpringForce:0}";
            SetSliderSilent(_rearSpringSlider, _tuning.RearSpringForce);
            if (_rearSpringValueText != null) _rearSpringValueText.text = $"{_tuning.RearSpringForce:0}";
            SetSliderSilent(_frontRideHeightSlider, _tuning.FrontRideHeight);
            if (_frontRideHeightValueText != null) _frontRideHeightValueText.text = $"{_tuning.FrontRideHeight:0.00}m";
            SetSliderSilent(_rearRideHeightSlider, _tuning.RearRideHeight);
            if (_rearRideHeightValueText != null) _rearRideHeightValueText.text = $"{_tuning.RearRideHeight:0.00}m";

            SetSliderSilent(_frontCamberSlider, _tuning.FrontCamber);
            if (_frontCamberValueText != null) _frontCamberValueText.text = $"{_tuning.FrontCamber:0.0}°";
            SetSliderSilent(_rearCamberSlider, _tuning.RearCamber);
            if (_rearCamberValueText != null) _rearCamberValueText.text = $"{_tuning.RearCamber:0.0}°";

            SetSliderSilent(_masterVolumeSlider, AudioListener.volume);
            if (_masterVolumeValueText != null) _masterVolumeValueText.text = $"{AudioListener.volume * 100f:0}%";
        }

        // ---- Silent setters (avoid re-firing OnValueChanged in refresh) ----
        private static void SetSliderSilent(Slider s, float v)
        {
            if (s == null) return;
            s.SetValueWithoutNotify(v);
        }
        private static void SetToggleSilent(Toggle t, bool v)
        {
            if (t == null) return;
            t.SetIsOnWithoutNotify(v);
        }
        private static void SetDropdownSilent(TMP_Dropdown d, int v)
        {
            if (d == null) return;
            d.SetValueWithoutNotify(v);
        }

        // ---- Snapshot / restore ----
        private void SnapshotDefaults()
        {
            _defaults = new TuningDefaults
            {
                Drivetrain = _tuning != null ? _tuning.Drivetrain : DrivetrainMode.Rwd,
                AutomaticTransmission = _tuning == null || _tuning.AutomaticTransmission,
                FrontSpringForce = _tuning != null ? _tuning.FrontSpringForce : 1200f,
                RearSpringForce = _tuning != null ? _tuning.RearSpringForce : 1000f,
                FrontRideHeight = _tuning != null ? _tuning.FrontRideHeight : 0f,
                RearRideHeight = _tuning != null ? _tuning.RearRideHeight : 0f,
                FrontCamber = _tuning != null ? _tuning.FrontCamber : 0f,
                RearCamber = _tuning != null ? _tuning.RearCamber : 0f,
                HelperValue = _tuning != null ? _tuning.HelperValue : 0.35f,
                MasterVolume = AudioListener.volume,
            };
        }

        public void ResetToDefaults()
        {
            if (_tuning != null)
            {
                _tuning.Drivetrain = _defaults.Drivetrain;
                _tuning.AutomaticTransmission = _defaults.AutomaticTransmission;
                _tuning.FrontSpringForce = _defaults.FrontSpringForce;
                _tuning.RearSpringForce = _defaults.RearSpringForce;
                _tuning.FrontRideHeight = _defaults.FrontRideHeight;
                _tuning.RearRideHeight = _defaults.RearRideHeight;
                _tuning.FrontCamber = _defaults.FrontCamber;
                _tuning.RearCamber = _defaults.RearCamber;
                _tuning.HelperValue = _defaults.HelperValue;
                _tuning.RaiseChanged();
            }
            AudioListener.volume = _defaults.MasterVolume;
            RefreshFromData();
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            var s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex);
        }
    }
}
