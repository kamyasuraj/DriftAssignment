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
        [Header("Ground-contact filter")]
        [Tooltip("Collisions with these layers are ignored. Set to include the terrain / road so belly-scrape + wheel-bounce during drift don't fire impact SFX or paint damage.")]
        [SerializeField] private LayerMask _ignoreLayers;
        [Tooltip("Reject a contact if its normal is within this angle of world-up (contact is 'from below', i.e. ground). Set to 0 to disable.")]
        [Range(0f, 89f)] [SerializeField] private float _rejectGroundNormalAngle = 40f;
        [SerializeField] private bool _debugLog = false;

        private IDamageable[] _damageables;

        /// (impulseMagnitude, worldContactPoint, worldSurfaceNormal) — fires once per
        /// qualifying collision AND once per DebugImpact call. Consumers: CarHealth,
        /// ImpactVfx, DamageCascade. The surface normal points AWAY from the car,
        /// so decals should orient facing +normal.
        public event Action<float, Vector3, Vector3> DamageDealt;

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

            // Ignore-layer filter (typically Terrain / road) — kills belly-scrape and
            // wheel-bounce spam during hard drifts.
            if (((1 << collision.gameObject.layer) & _ignoreLayers.value) != 0)
            {
                if (_debugLog) Debug.Log($"[ImpactReceiver] Rejected {collision.gameObject.name} on ignored layer {LayerMask.LayerToName(collision.gameObject.layer)}", this);
                return;
            }

            var impulse = collision.impulse;
            var mag = impulse.magnitude;
            if (mag < _config.MinDentImpulse)
            {
                if (_debugLog) Debug.Log($"[ImpactReceiver] Rejected {collision.gameObject.name} impulse={mag:F2} < min {_config.MinDentImpulse}", this);
                return;
            }

            var contact = collision.GetContact(0);

            // Ground-normal filter: if the contact normal is close to world-up, the
            // car body has kissed the ground (or a very flat surface underneath) —
            // that's not a "you hit a wall" event.
            if (_rejectGroundNormalAngle > 0f)
            {
                var upDot = Vector3.Dot(contact.normal, Vector3.up);
                var thresholdCos = Mathf.Cos(_rejectGroundNormalAngle * Mathf.Deg2Rad);
                if (upDot > thresholdCos)
                {
                    if (_debugLog) Debug.Log($"[ImpactReceiver] Rejected {collision.gameObject.name} — ground-like contact (normal.y={upDot:F2})", this);
                    return;
                }
            }

            int routed = 0;
            foreach (var d in _damageables)
            {
                if (d == null) continue;
                if (!d.IsInRange(contact.point)) continue;
                d.ReceiveImpact(contact.point, impulse, mag);
                routed++;
            }
            DamageDealt?.Invoke(mag, contact.point, contact.normal);
            if (_debugLog) Debug.Log($"[ImpactReceiver] Hit {collision.gameObject.name} impulse={mag:F1} at {contact.point} normal={contact.normal} → routed to {routed} damageables", this);
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
            // For scripted hits the outward-facing surface normal is the opposite of
            // the inward push direction fed by the test panel.
            var normal = -worldInwardDir.normalized;
            DamageDealt?.Invoke(magnitude, worldContact, normal);
            if (_debugLog) Debug.Log($"[ImpactReceiver] DEBUG impact at {worldContact} mag={magnitude:F1} → routed to {routed} damageables", this);
            return routed;
        }
    }
}
