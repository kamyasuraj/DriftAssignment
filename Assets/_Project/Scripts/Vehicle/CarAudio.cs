using System.Collections;
using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    /// Engine audio — RPM-driven 4-layer crossfade mixer.
    /// Layers: Idle → Low → Med → High, each with a triangular volume envelope
    /// centered on a normalized RPM and a per-layer pitch that shifts as RPM
    /// moves through the layer (granular pitch modulation). Sounds are the
    /// off-throttle loops from the Rotary X8 Free pack.
    ///
    /// Engine kicks in automatically at scene start after the car settles —
    /// no player input required. Blip/rev-brake logic removed.
    [DisallowMultipleComponent]
    public class CarAudio : MonoBehaviour
    {
        private enum EngineState { Off, Starting, Running }

        [Header("Refs")]
        [SerializeField] private CarController _car;
        [SerializeField] private CarConfig _config;
        [SerializeField] private SoundLibrary _sounds;

        [Header("Audio Sources")]
        [Tooltip("One-shot: EngineStart (startup)")]
        [SerializeField] private AudioSource _startSource;
        [Tooltip("Loop: EngineIdle (peaks at MinRpm)")]
        [SerializeField] private AudioSource _idleSource;
        [Tooltip("Loop: Low RPM")]
        [SerializeField] private AudioSource _lowSource;
        [Tooltip("Loop: Med RPM")]
        [SerializeField] private AudioSource _medSource;
        [Tooltip("Loop: High RPM")]
        [SerializeField] private AudioSource _highSource;
        [SerializeField] private AudioSource _tireSource;
        [SerializeField] private AudioSource _impactSource;
        [Tooltip("One-shot: HandBrake press")]
        [SerializeField] private AudioSource _handbrakeSource;

        [Header("Auto-start")]
        [Tooltip("Delay after Awake before engine startup fires — lets the car physics settle onto the ground.")]
        [SerializeField] private float _autoStartDelaySec = 0.8f;

        [Header("RPM layer crossfade (normalized 0..1)")]
        [Range(0f, 1f)] [SerializeField] private float _idleCenter = 0f;
        [Range(0.05f, 1f)] [SerializeField] private float _idleWidth = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _lowCenter = 0.28f;
        [Range(0.05f, 1f)] [SerializeField] private float _lowWidth = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float _medCenter = 0.6f;
        [Range(0.05f, 1f)] [SerializeField] private float _medWidth = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _highCenter = 0.92f;
        [Range(0.05f, 1f)] [SerializeField] private float _highWidth = 0.35f;

        [Header("Speed → mixer drive")]
        [Tooltip("Speed at which the layer-drive signal reaches 1.0 — treat as 'cruising speed where the high loop should be dominant'.")]
        [SerializeField] private float _speedReferenceKmh = 120f;
        [Tooltip("How much of the layer volume comes from speed vs. RPM. 0 = pure RPM (realistic), 1 = pure speed (arcadey). 0.6 gives the perception that revs climb with speed even when the auto-box upshifts early.")]
        [Range(0f, 1f)] [SerializeField] private float _speedBlend = 0.65f;

        [Header("Pitch modulation (granular feel)")]
        [Tooltip("Base pitch each loop plays at when at its center RPM.")]
        [SerializeField] private float _basePitch = 1.0f;
        [Tooltip("Amount pitch shifts per unit of normalized-RPM distance from the layer center.")]
        [SerializeField] private float _pitchPerRpm = 0.9f;
        [Tooltip("Clamp for extreme pitch swings.")]
        [SerializeField] private Vector2 _pitchClamp = new Vector2(0.7f, 1.6f);
        [Tooltip("Master gain applied on top of all layers.")]
        [Range(0f, 2f)] [SerializeField] private float _masterVolume = 1f;
        [Tooltip("How quickly layer volumes move toward their target each second.")]
        [SerializeField] private float _volumeSmoothing = 12f;
        [Tooltip("How quickly pitches move toward their target each second.")]
        [SerializeField] private float _pitchSmoothing = 8f;

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
        private float _lastImpactTime = -999f;
        private bool _handBrakeLastFrame;

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
            if (_idleSource == null || _lowSource == null || _medSource == null || _highSource == null
                || _startSource == null || _tireSource == null || _impactSource == null)
            {
                Debug.LogError("[CarAudio] One or more AudioSources not assigned.", this);
                enabled = false;
                return;
            }

            ConfigureLoop(_idleSource, _sounds.EngineIdle);
            ConfigureLoop(_lowSource, _sounds.EngineLow);
            ConfigureLoop(_medSource, _sounds.EngineMed);
            ConfigureLoop(_highSource, _sounds.EngineHigh);

            _startSource.playOnAwake = false;
            _startSource.spatialBlend = 0f;

            var tireClip = _sounds.TireScreech != null ? _sounds.TireScreech : _sounds.DriftBrakingCornering;
            if (tireClip != null)
            {
                _tireSource.clip = tireClip;
                _tireSource.loop = true;
                _tireSource.playOnAwake = false;
                _tireSource.spatialBlend = 0f;
                _tireSource.volume = 0f;
                _tireSource.Play();
            }

            if (_handbrakeSource != null)
            {
                _handbrakeSource.playOnAwake = false;
                _handbrakeSource.loop = false;
                _handbrakeSource.spatialBlend = 0f;
            }

            if (_debugLog) Debug.Log("[CarAudio] Awake — engine will auto-start after settle delay.", this);
        }

        private static void ConfigureLoop(AudioSource src, AudioClip clip)
        {
            src.Stop();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            src.spatialBlend = 0f;
        }

        private void Start()
        {
            StartCoroutine(AutoStart());
        }

        private IEnumerator AutoStart()
        {
            // Let physics settle so the startup sound doesn't overlap the
            // spawn-drop-thud collision.
            yield return new WaitForSeconds(_autoStartDelaySec);
            yield return StartupSequence();
        }

        private IEnumerator StartupSequence()
        {
            _state = EngineState.Starting;
            if (_debugLog) Debug.Log("[CarAudio] Engine → Starting", this);

            if (_sounds.EngineStart != null)
            {
                _startSource.PlayOneShot(_sounds.EngineStart);
                // Overlap the idle+layer loops onto the tail of the start clip
                yield return new WaitForSeconds(_sounds.EngineStart.length * 0.85f);
            }

            StartLoopSilent(_idleSource);
            StartLoopSilent(_lowSource);
            StartLoopSilent(_medSource);
            StartLoopSilent(_highSource);

            _state = EngineState.Running;
            if (_debugLog) Debug.Log("[CarAudio] Engine → Running (mixer live)", this);
        }

        private static void StartLoopSilent(AudioSource src)
        {
            if (src == null || src.clip == null) return;
            src.volume = 0f;
            src.pitch = 1f;
            src.Play();
        }

        private void Update()
        {
            if (_state != EngineState.Running) return;
            UpdateEngineMixer();
            UpdateTire();
            UpdateHandBrake();
        }

        private void UpdateHandBrake()
        {
            if (_car == null) return;
            var pressed = _car.HandBrakeActive;
            if (pressed && !_handBrakeLastFrame && _handbrakeSource != null && _sounds.HandBrake != null)
            {
                _handbrakeSource.PlayOneShot(_sounds.HandBrake);
                if (_debugLog) Debug.Log("[CarAudio] HandBrake one-shot", this);
            }
            _handBrakeLastFrame = pressed;
        }

        private void UpdateEngineMixer()
        {
            if (_config == null) return;
            var rpmNorm = Mathf.InverseLerp(_config.MinRpm, _config.MaxRpm, _car.EngineRpm);
            var speedNorm = Mathf.Clamp01(_car.SpeedKmh / Mathf.Max(1f, _speedReferenceKmh));
            // Hybrid drive: blends RPM (realistic) with speed (perceptual — kicks
            // driving loops in even when the auto box holds RPM low at cruise).
            var drive = Mathf.Lerp(rpmNorm, Mathf.Max(rpmNorm, speedNorm), _speedBlend);
            var dt = Time.deltaTime;

            ApplyLayer(_idleSource, drive, rpmNorm, _idleCenter, _idleWidth, dt);
            ApplyLayer(_lowSource, drive, rpmNorm, _lowCenter, _lowWidth, dt);
            ApplyLayer(_medSource, drive, rpmNorm, _medCenter, _medWidth, dt);
            ApplyLayer(_highSource, drive, rpmNorm, _highCenter, _highWidth, dt);
        }

        private void ApplyLayer(AudioSource src, float drive, float rpmNorm, float center, float width, float dt)
        {
            if (src == null || !src.isPlaying) return;
            // Volume envelope drives off the hybrid drive signal so speed can
            // push driving loops up even when RPM is low.
            var half = Mathf.Max(0.01f, width * 0.5f);
            var dist = Mathf.Abs(drive - center);
            var targetVolume = Mathf.Clamp01(1f - dist / half) * _masterVolume;
            targetVolume = Mathf.SmoothStep(0f, 1f, targetVolume);

            // Pitch still driven by actual RPM so it stays realistic.
            var pitchShift = (rpmNorm - center) * _pitchPerRpm;
            var targetPitch = Mathf.Clamp(_basePitch + pitchShift, _pitchClamp.x, _pitchClamp.y);

            src.volume = Mathf.Lerp(src.volume, targetVolume, dt * _volumeSmoothing);
            src.pitch = Mathf.Lerp(src.pitch, targetPitch, dt * _pitchSmoothing);
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
