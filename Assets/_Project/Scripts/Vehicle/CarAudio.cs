using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Vehicle
{
    [DisallowMultipleComponent]
    public class CarAudio : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CarController _car;
        [SerializeField] private CarConfig _config;
        [SerializeField] private SoundLibrary _sounds;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _engineSource;
        [SerializeField] private AudioSource _tireSource;
        [SerializeField] private AudioSource _impactSource;

        [Header("Engine Pitch/Volume Mapping")]
        [Range(0.2f, 1.5f)] [SerializeField] private float _minEnginePitch = 0.55f;
        [Range(0.5f, 2.5f)] [SerializeField] private float _maxEnginePitch = 1.6f;
        [Range(0f, 1f)] [SerializeField] private float _minEngineVolume = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float _maxEngineVolume = 0.9f;

        [Header("Tire — Drift Screech")]
        [Range(0f, 1f)] [SerializeField] private float _tireSlipThreshold = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float _tireMaxVolume = 0.8f;
        [SerializeField] private float _tireVolumeSmoothing = 6f;

        [Header("Impact Thresholds (impulse magnitude)")]
        [SerializeField] private float _lightImpactThreshold = 3f;
        [SerializeField] private float _mediumImpactThreshold = 8f;
        [SerializeField] private float _heavyImpactThreshold = 20f;
        [SerializeField] private float _impactCooldownSeconds = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Rigidbody _rigidbody;
        private float _lastImpactTime;

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
            if (_engineSource == null || _tireSource == null || _impactSource == null)
            {
                Debug.LogError("[CarAudio] One or more AudioSources not assigned.", this);
                enabled = false;
                return;
            }

            // Belt-and-braces: ensure engine + tire are playing even if pre-configured differently
            if (!_engineSource.isPlaying) _engineSource.Play();
            if (!_tireSource.isPlaying) _tireSource.Play();

            if (_debugLog)
            {
                Debug.Log($"[CarAudio] Awake OK. Engine clip='{(_engineSource.clip != null ? _engineSource.clip.name : "NULL")}' " +
                          $"Tire clip='{(_tireSource.clip != null ? _tireSource.clip.name : "NULL")}' " +
                          $"AudioListener present={UnityEngine.Object.FindFirstObjectByType<AudioListener>() != null}", this);
            }
        }

        private void Update()
        {
            if (_car == null || _sounds == null) return;
            UpdateEngine();
            UpdateTire();
        }

        private void UpdateEngine()
        {
            if (_engineSource == null) return;
            var minRpm = _config != null ? _config.MinRpm : 900f;
            var maxRpm = _config != null ? _config.MaxRpm : 7500f;
            var rpmNorm = Mathf.InverseLerp(minRpm, maxRpm, _car.EngineRpm);
            _engineSource.pitch = Mathf.Lerp(_minEnginePitch, _maxEnginePitch, rpmNorm);
            _engineSource.volume = Mathf.Lerp(_minEngineVolume, _maxEngineVolume, rpmNorm);
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
