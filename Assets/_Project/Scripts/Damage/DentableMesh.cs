using DriftAssignment.Core;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Runtime vertex displacement in a radius around each contact.
    /// Attach to a GameObject that has a MeshFilter (a visible car panel).
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class DentableMesh : MonoBehaviour, IDamageable
    {
        [SerializeField] private DamageConfig _config;
        [SerializeField] private float _boundsPaddingWorld = 0.1f;
        [SerializeField] private bool _debugLog = false;

        private MeshFilter _filter;
        private Renderer _renderer;
        private Mesh _mesh;
        private Vector3[] _originalVerts;
        private Vector3[] _currentVerts;

        public int LastMutatedVerts { get; private set; }
        public int TotalVertexCount => _originalVerts != null ? _originalVerts.Length : 0;

        private void Awake()
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<Renderer>();
            if (_filter.sharedMesh == null) { enabled = false; return; }

            if (!_filter.sharedMesh.isReadable)
            {
                Debug.LogError($"[DentableMesh] {name} source mesh '{_filter.sharedMesh.name}' has Read/Write disabled — enable it on the FBX importer or dents cannot apply. Disabling.", this);
                enabled = false;
                return;
            }

            _mesh = Instantiate(_filter.sharedMesh);
            _mesh.name = _filter.sharedMesh.name + " (Dentable Instance)";
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;

            _originalVerts = _mesh.vertices;
            _currentVerts = (Vector3[])_originalVerts.Clone();
        }

        public bool IsInRange(Vector3 pointWorld)
        {
            if (_mesh == null || _renderer == null) return false;
            var radius = _config != null ? _config.DentRadius : 0.5f;
            var b = _renderer.bounds;
            b.Expand((radius + _boundsPaddingWorld) * 2f);
            return b.Contains(pointWorld);
        }

        public void ReceiveImpact(Vector3 contactPointWorld, Vector3 impulse, float impulseMagnitude)
        {
            if (_config == null || _originalVerts == null) return;
            if (impulseMagnitude < _config.MinDentImpulse) return;

            var localContact = transform.InverseTransformPoint(contactPointWorld);
            var localImpulseDir = transform.InverseTransformDirection(impulse.normalized);
            var radius = _config.DentRadius;
            var maxDepth = Mathf.Min(_config.DentMaxDepth, impulseMagnitude * _config.DentImpulseScale);
            var radiusSq = radius * radius;
            var mutated = 0;

            for (int i = 0; i < _currentVerts.Length; i++)
            {
                var offset = _currentVerts[i] - localContact;
                var distSq = offset.sqrMagnitude;
                if (distSq > radiusSq) continue;
                var dist = Mathf.Sqrt(distSq);
                var falloff = 1f - Mathf.Pow(dist / radius, _config.DentFalloff);
                _currentVerts[i] += localImpulseDir * (maxDepth * falloff);
                mutated++;
            }

            LastMutatedVerts = mutated;
            if (mutated == 0)
            {
                if (_config.DebugLog || _debugLog) Debug.Log($"[DentableMesh] {name} in-range but 0 verts within radius (contact too far) impulse={impulseMagnitude:F1}", this);
                return;
            }
            _mesh.vertices = _currentVerts;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (_config.DebugLog || _debugLog) Debug.Log($"[DentableMesh] {name} dented {mutated} verts impulse={impulseMagnitude:F1} depth={maxDepth:F3}", this);
        }

        public void ResetToOriginal()
        {
            if (_originalVerts == null || _mesh == null || _currentVerts == null) return;
            System.Array.Copy(_originalVerts, _currentVerts, _originalVerts.Length);
            _mesh.vertices = _currentVerts;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            LastMutatedVerts = 0;
        }
    }
}
