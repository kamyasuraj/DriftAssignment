using System.Collections;
using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [DisallowMultipleComponent]
    public class CarAudio : MonoBehaviour
    {
        private enum EngineState { Off, Starting, Running }

        [Header("Refs")]
        [SerializeField] private CarController _car;
        [SerializeField] private CarConfig _config;
        [SerializeField] private SoundLibrary _sounds;

        [Header("Audio Sources")]
        [Tooltip("Loops EngineIdle after startup")]
        [SerializeField] private AudioSource _engineLoopSource;
        [Tooltip("PlayOneShot for EngineStart and blips (overlays the loop cleanly)")]
        [SerializeField] private AudioSource _engineOneShotSource;
        [SerializeField] private AudioSource _tireSource;
        [SerializeField] private AudioSource _impactSource;

        [Header("Idle volume mapping (subtle rise with RPM)")]
        [Range(0f, 1f)] [SerializeField] private float _idleMinVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _idleMaxVolume = 0.7f;

        [Header("Throttle tap → blip detection")]
        [SerializeField] private float _tapMaxDurationSec = 0.4f;
        [SerializeField] private float _tapThreshold = 0.2f;
        [SerializeField] private float _tapMinPeak = 0.3f;

        [Header("Rev while braking")]
        [SerializeField] private float _revBrakeThrottleMin = 0.4f;
        [SerializeField] private float _revBrakeBrakeMin = 0.5f;
        [SerializeField] private float _revBrakeCooldownSec = 1.2f;

        [Header("Tire — Drift Screech")]
        [Range(0f, 1f)] [SerializeField] private float _tireSlipThreshold = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _tireMaxVolume = 0.8f;
        [SerializeField] private float _tireVolumeSmoothing = 6f;

        [Header("Impact")]
        [SerializeField] private float _lightImpactThreshold = 3f;
        [SerializeField] private float _mediumImpactThreshold = 8f;
        [SerializeField] private float _heavyImpactThreshold = 20f;
        [SerializeField] private float _impactCooldownSeconds = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Rigidbody _rigidbody;
        private EngineState _state = EngineState.Off;

        // Tap detection
        private bool _throttleActive;
        private float _throttleRiseTime;
        private float _throttlePeak;

        // Rev-brake cooldown
        private float _lastRevBrakeTime = -999f;

        // Impact cooldown
        private float _lastImpactTime = -999f;

        private void Awake()
        {
            if (_car == null) _car = GetComponent<CarController>();
            _rigidbody = GetComponent<Rigidbody>();

            if (_sounds == null)
            {
                Debug.LogError("[CarAudio] SoundLibrary not assigned.", this);
                enabled = false;
                return;
            }
            if (_engineLoopSource == null || _engineOneShotSource == null || _tireSource == null || _impactSource == null)
            {
                Debug.LogError("[CarAudio] One or more AudioSources not assigned.", this);
                enabled = false;
                return;
            }

            // Stop everything at start — engine is off until first input
            _engineLoopSource.Stop();
            _engineLoopSource.clip = _sounds.EngineIdle;
            _engineLoopSource.loop = true;
            _engineLoopSource.playOnAwake = false;
            _engineLoopSource.volume = _idleMinVolume;
            _engineLoopSource.spatialBlend = 0f;

            _engineOneShotSource.playOnAwake = false;
            _engineOneShotSource.spatialBlend = 0f;

            // Tire screech clip primed (silent until slip detected)
            if (_sounds.DriftBrakingCornering != null)
            {
                _tireSource.clip = _sounds.DriftBrakingCornering;
                _tireSource.loop = true;
                _tireSource.playOnAwake = false;
                _tireSource.spatialBlend = 0f;
                _tireSource.volume = 0f;
                _tireSource.Play();
            }

            if (_debugLog) Debug.Log("[CarAudio] Awake. Engine OFF. Waiting for first input.", this);
        }

        private void Update()
        {
            switch (_state)
            {
                case EngineState.Off:
                    if (AnyInputActive()) StartCoroutine(StartupSequence());
                    break;
                case EngineState.Running:
                    UpdateBlipDetection();
                    UpdateIdleVolume();
                    UpdateTire();
                    break;
            }
        }

        private bool AnyInputActive()
        {
            return _car.ThrottleInput > 0.1f
                || _car.BrakeInput > 0.1f
                || _car.HandBrakeActive
                || Mathf.Abs(_car.SteerInput) > 0.1f;
        }

        private IEnumerator StartupSequence()
        {
            _state = EngineState.Starting;
            if (_debugLog) Debug.Log("[CarAudio] Engine → Starting", this);

            if (_sounds.EngineStart != null)
            {
                _engineOneShotSource.PlayOneShot(_sounds.EngineStart);
                // Start the idle loop 85% through the start clip so they blend
                yield return new WaitForSeconds(_sounds.EngineStart.length * 0.85f);
            }

            if (_engineLoopSource.clip != null)
            {
                _engineLoopSource.volume = _idleMinVolume;
                _engineLoopSource.Play();
            }

            _state = EngineState.Running;
            if (_debugLog) Debug.Log("[CarAudio] Engine → Running (idle loop started)", this);
        }

        private void UpdateBlipDetection()
        {
            var throttle = _car.ThrottleInput;
            var brake = _car.BrakeInput;

            // Rev while braking — brake + throttle held simultaneously → periodic blip
            if (brake > _revBrakeBrakeMin && throttle > _revBrakeThrottleMin)
            {
                if (Time.time - _lastRevBrakeTime > _revBrakeCooldownSec)
                {
                    PlayBlip(throttle, "rev-brake");
                    _lastRevBrakeTime = Time.time;
                }
                _throttleActive = false; // don't also fire tap blip
                return;
            }

            // Throttle tap detection: rising edge → peak → release
            if (throttle > _tapThreshold && !_throttleActive)
            {
                _throttleActive = true;
                _throttleRiseTime = Time.time;
                _throttlePeak = throttle;
            }
            else if (_throttleActive)
            {
                _throttlePeak = Mathf.Max(_throttlePeak, throttle);

                // Held too long → not a tap, cancel silently (they're actually driving)
                if (Time.time - _throttleRiseTime > _tapMaxDurationSec && throttle > _tapThreshold)
                {
                    _throttleActive = false;
                }
                // Release within window → fire blip
                else if (throttle < 0.1f)
                {
                    _throttleActive = false;
                    var dur = Time.time - _throttleRiseTime;
                    if (dur < _tapMaxDurationSec && _throttlePeak > _tapMinPeak)
                        PlayBlip(_throttlePeak, "tap");
                }
            }
        }

        private void PlayBlip(float intensity, string reason)
        {
            if (_sounds.EngineBlips == null || _sounds.EngineBlips.Length == 0) return;
            int idx;
            if (intensity < 0.5f) idx = 0;
            else if (intensity < 0.85f) idx = 1;
            else idx = 2;
            idx = Mathf.Clamp(idx, 0, _sounds.EngineBlips.Length - 1);

            var clip = _sounds.EngineBlips[idx];
            if (clip == null) return;
            _engineOneShotSource.PlayOneShot(clip);

            if (_debugLog) Debug.Log($"[CarAudio] Blip {idx} ({reason}, intensity={intensity:F2})", this);
        }

        private void UpdateIdleVolume()
        {
            if (_engineLoopSource == null || _config == null) return;
            var rpmNorm = Mathf.InverseLerp(_config.MinRpm, _config.MaxRpm, _car.EngineRpm);
            _engineLoopSource.volume = Mathf.Lerp(_idleMinVolume, _idleMaxVolume, rpmNorm);
        }

        private void UpdateTire()
        {
            if (_tireSource == null || _rigidbody == null) return;
            var localVel = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            var forwardSpeed = Mathf.Abs(localVel.z);
            var sidewaysSpeed = Mathf.Abs(localVel.x);
            var target = 0f;
            if (forwardSpeed > 2f)
            {
                var slip = sidewaysSpeed / (forwardSpeed + 0.1f);
                if (slip > _tireSlipThreshold) target = Mathf.Clamp01(slip * 1.5f) * _tireMaxVolume;
            }
            _tireSource.volume = Mathf.Lerp(_tireSource.volume, target, Time.deltaTime * _tireVolumeSmoothing);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_sounds == null || _impactSource == null) return;
            if (Time.time - _lastImpactTime < _impactCooldownSeconds) return;

            var impulse = collision.impulse.magnitude;
            AudioClip[] bank = null;
            string tier = null;
            if (impulse > _heavyImpactThreshold) { bank = _sounds.MetalHeavy; tier = "Heavy"; }
            else if (impulse > _mediumImpactThreshold) { bank = _sounds.MetalMedium; tier = "Medium"; }
            else if (impulse > _lightImpactThreshold) { bank = _sounds.MetalLight; tier = "Light"; }
            else return;

            var clip = _sounds.PickRandom(bank);
            if (clip == null) return;
            _impactSource.PlayOneShot(clip);
            _lastImpactTime = Time.time;

            if (_debugLog) Debug.Log($"[CarAudio] Impact impulse={impulse:F1} → {tier}", this);
        }
    }
}
