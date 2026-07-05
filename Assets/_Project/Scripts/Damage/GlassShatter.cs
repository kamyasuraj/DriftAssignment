using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Glass panels: on heavy impact, hide the mesh (MVP). Later can be
    /// upgraded to shader swap + shard particle burst.
    [DisallowMultipleComponent]
    public class GlassShatter : MonoBehaviour, IDamageable
    {
        [SerializeField] private DamageConfig _config;
        [SerializeField] private float _boundsPaddingWorld = 0.05f;
        [SerializeField] private bool _debugLog = false;

        private bool _shattered;
        private Bounds _worldBounds;
        private bool _hasBounds;

        private void Awake()
        {
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>();
            if (r != null) { _worldBounds = r.bounds; _hasBounds = true; }
        }

        private void LateUpdate()
        {
            if (_shattered || !_hasBounds) return;
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>();
            if (r != null) _worldBounds = r.bounds;
        }

        public bool IsInRange(Vector3 pointWorld)
        {
            if (_shattered || !_hasBounds) return false;
            var expanded = _worldBounds;
            expanded.Expand(_boundsPaddingWorld * 2f);
            return expanded.Contains(pointWorld);
        }

        public void ReceiveImpact(Vector3 contactPointWorld, Vector3 impulse, float impulseMagnitude)
        {
            if (_shattered || _config == null) return;
            if (impulseMagnitude < _config.GlassShatterThreshold) return;
            Shatter("impact");
        }

        /// External trigger (e.g. DamageCascade at a health threshold).
        public void Shatter(string reason)
        {
            if (_shattered) return;
            _shattered = true;
            gameObject.SetActive(false);
            if ((_config != null && _config.DebugLog) || _debugLog) Debug.Log($"[GlassShatter] {name} shattered ({reason})", this);
        }
    }
}
