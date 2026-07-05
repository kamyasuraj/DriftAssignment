using System;
using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Attach to car root. Listens to OnCollisionEnter, finds every IDamageable
    /// child whose bounds contain the contact point, and forwards the impulse.
    [DisallowMultipleComponent]
    public class ImpactReceiver : MonoBehaviour
    {
        [SerializeField] private DamageConfig _config;
        [SerializeField] private bool _debugLog = false;

        private IDamageable[] _damageables;

        /// (impulseMagnitude, worldContactPoint) — fires once per qualifying collision
        /// AND once per DebugImpact call. Consumers: CarHealth, ImpactVfx.
        public event Action<float, Vector3> DamageDealt;

        private void Awake()
        {
            RefreshDamageables();
        }

        public void RefreshDamageables()
        {
            _damageables = GetComponentsInChildren<IDamageable>(true);
            if (_debugLog) Debug.Log($"[ImpactReceiver] Registered {_damageables.Length} damageables", this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_config == null || _damageables == null) return;
            var impulse = collision.impulse;
            var mag = impulse.magnitude;
            if (mag < _config.MinDentImpulse)
            {
                if (_debugLog) Debug.Log($"[ImpactReceiver] Rejected {collision.gameObject.name} impulse={mag:F2} < min {_config.MinDentImpulse}", this);
                return;
            }

            var contact = collision.GetContact(0);
            int routed = 0;
            foreach (var d in _damageables)
            {
                if (d == null) continue;
                if (!d.IsInRange(contact.point)) continue;
                d.ReceiveImpact(contact.point, impulse, mag);
                routed++;
            }
            DamageDealt?.Invoke(mag, contact.point);
            if (_debugLog) Debug.Log($"[ImpactReceiver] Hit {collision.gameObject.name} impulse={mag:F1} at {contact.point} → routed to {routed} damageables", this);
        }

        /// Bypasses OnCollisionEnter — routes a synthetic impact at a world-space
        /// contact point with a fabricated impulse. For the DamageTestPanel.
        public int DebugImpact(Vector3 worldContact, Vector3 worldInwardDir, float magnitude)
        {
            if (_config == null || _damageables == null) return 0;
            var impulse = worldInwardDir.normalized * magnitude;
            int routed = 0;
            foreach (var d in _damageables)
            {
                if (d == null) continue;
                if (!d.IsInRange(worldContact)) continue;
                d.ReceiveImpact(worldContact, impulse, magnitude);
                routed++;
            }
            DamageDealt?.Invoke(magnitude, worldContact);
            if (_debugLog) Debug.Log($"[ImpactReceiver] DEBUG impact at {worldContact} mag={magnitude:F1} → routed to {routed} damageables", this);
            return routed;
        }
    }
}
