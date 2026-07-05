using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Accumulates impact; on threshold, unparents + adds Rigidbody, inherits car
    /// velocity + applies impact impulse, and despawns after N seconds.
    [DisallowMultipleComponent]
    public class DetachablePart : MonoBehaviour, IDamageable
    {
        [SerializeField] private DamageConfig _config;
        [SerializeField] private float _boundsPaddingWorld = 0.1f;
        [SerializeField] private bool _debugLog = false;

        private float _accumulated;
        private bool _detached;
        private Rigidbody _carBody;
        private Bounds _worldBounds;
        private bool _hasBounds;

        private void Awake()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null) { _worldBounds = renderer.bounds; _hasBounds = true; }
            _carBody = GetComponentInParent<Rigidbody>();
        }

        private void LateUpdate()
        {
            if (_detached || !_hasBounds) return;
            var r = GetComponentInChildren<Renderer>();
            if (r != null) _worldBounds = r.bounds;
        }

        public bool IsInRange(Vector3 pointWorld)
        {
            if (_detached || !_hasBounds) return false;
            var expanded = _worldBounds;
            expanded.Expand(_boundsPaddingWorld * 2f);
            return expanded.Contains(pointWorld);
        }

        public void ReceiveImpact(Vector3 contactPointWorld, Vector3 impulse, float impulseMagnitude)
        {
            if (_detached || _config == null) return;
            _accumulated += impulseMagnitude;
            if (_debugLog) Debug.Log($"[DetachablePart] {name} accum={_accumulated:F1}/{_config.BreakThreshold}", this);
            if (_accumulated >= _config.BreakThreshold) Detach(impulse);
        }

        private void Detach(Vector3 impulse)
        {
            _detached = true;
            transform.SetParent(null, true);

            // Ensure a collider — prefer converting any MeshCollider to convex,
            // else add a BoxCollider sized to bounds so it participates in physics.
            var mc = GetComponentInChildren<MeshCollider>();
            if (mc != null) { mc.convex = true; }
            else if (GetComponentInChildren<Collider>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                if (_hasBounds)
                {
                    box.center = transform.InverseTransformPoint(_worldBounds.center);
                    box.size = _worldBounds.size * 0.9f;
                }
            }

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = _config.DetachedPartMass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (_carBody != null) rb.linearVelocity = _carBody.linearVelocity;
            var kick = impulse.normalized * (_accumulated * _config.DetachImpulseScale);
            rb.AddForce(kick, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * _accumulated * 0.1f, ForceMode.Impulse);

            if (_config.DebugLog || _debugLog) Debug.Log($"[DetachablePart] {name} DETACHED (accum={_accumulated:F1})", this);
            Destroy(gameObject, _config.DetachedLifetime);
        }
    }
}
