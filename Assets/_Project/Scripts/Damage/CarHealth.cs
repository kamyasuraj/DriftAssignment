using System;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Tracks cumulative damage. Subscribes to ImpactReceiver's DamageDealt
    /// event and converts impulse into a 0..1 health drop. Exposed for UI.
    [DisallowMultipleComponent]
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private ImpactReceiver _receiver;
        [Tooltip("Total impulse the car can absorb before reaching 0 health")]
        [SerializeField] private float _maxDamageImpulse = 4000f;
        [SerializeField] private bool _debugLog = false;

        public float MaxDamageImpulse => _maxDamageImpulse;
        public float AccumulatedDamage { get; private set; }
        public float HealthNormalized => Mathf.Clamp01(1f - AccumulatedDamage / Mathf.Max(_maxDamageImpulse, 1f));
        public float HealthPercent => HealthNormalized * 100f;

        public event Action<float> HealthChanged; // fires with new HealthNormalized

        private void Awake()
        {
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
        }

        private void OnEnable()
        {
            if (_receiver != null) _receiver.DamageDealt += OnDamage;
        }

        private void OnDisable()
        {
            if (_receiver != null) _receiver.DamageDealt -= OnDamage;
        }

        private void OnDamage(float impulseMagnitude, Vector3 worldContact, Vector3 worldNormal)
        {
            AccumulatedDamage += impulseMagnitude;
            HealthChanged?.Invoke(HealthNormalized);
            if (_debugLog) Debug.Log($"[CarHealth] +{impulseMagnitude:F0} → total {AccumulatedDamage:F0} / {_maxDamageImpulse:F0} ({HealthPercent:F0}%)", this);
        }

        public void ResetHealth()
        {
            AccumulatedDamage = 0f;
            HealthChanged?.Invoke(HealthNormalized);
        }
    }
}
