using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Real paint spoilage — paints dark scuffs directly into a runtime clone
    /// of the paint material's albedo texture at the UVs of impacted verts.
    /// No floating decal quads. Damage is baked into the surface itself, so it
    /// perfectly follows dents / deformation / detachment.
    ///
    /// Prereq: the paint texture's ImportSettings must have Read/Write enabled.
    [DisallowMultipleComponent]
    public class PaintDamage : MonoBehaviour
    {
        [SerializeField] private ImpactReceiver _receiver;
        [SerializeField] private DamageConfig _config;
        [Tooltip("Shared paint material — its _BaseMap will be replaced with a runtime clone.")]
        [SerializeField] private Material _paintMaterial;
        [Tooltip("Only meshes whose name contains this substring will contribute UVs (avoids denting into unrelated atlas regions).")]
        [SerializeField] private string _paintMeshFilter = "Paint";

        [Header("Brush — sharp scratch lines")]
        [Tooltip("Color painted into the texture where damage occurs (near black = bare metal)")]
        [SerializeField] private Color _damageColor = new Color(0.05f, 0.04f, 0.04f, 1f);
        [Tooltip("Pixel radius of the scatter area — scratches are placed randomly within this circle around the impact UV. Keep small so scratches stay in this panel's UV island.")]
        [SerializeField] private int _scatterRadius = 7;
        [Tooltip("Total number of scratches per cluster (mix of short + long)")]
        [SerializeField] private int _scratchesMin = 3;
        [SerializeField] private int _scratchesMax = 6;
        [Tooltip("Short scratch length range (px)")]
        [SerializeField] private int _shortLenMin = 8;
        [SerializeField] private int _shortLenMax = 20;
        [Tooltip("Long drag-mark length range (px)")]
        [SerializeField] private int _longLenMin = 25;
        [SerializeField] private int _longLenMax = 55;
        [Tooltip("Fraction of scratches that are long drag marks (0..1)")]
        [Range(0f, 1f)] [SerializeField] private float _longScratchFraction = 0.3f;
        [Tooltip("How opaque each stroke is (0=none, 1=full replace)")]
        [Range(0f, 1f)] [SerializeField] private float _brushIntensity = 0.95f;
        [Tooltip("World-space radius (multiplier on DentRadius) — verts within this range around the contact get their UV painted")]
        [SerializeField] private float _paintRadiusMultiplier = 1.25f;
        [Tooltip("Maximum scratch clusters painted per panel per hit — spreads across UV islands without runaway cost")]
        [SerializeField] private int _maxClustersPerPanel = 6;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Texture2D _damageTexture;
        private Color[] _originalPixels;
        private DentableMesh[] _dentables;
        private Material _runtimeMaterial;
        private System.Collections.Generic.List<Renderer> _touchedRenderers = new System.Collections.Generic.List<Renderer>();

        public Texture2D DamageTexture => _damageTexture;

        private void Awake()
        {
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
            _dentables = GetComponentsInChildren<DentableMesh>(true);
            CloneMaterialAndAssign();
        }

        private void OnEnable()
        {
            if (_receiver != null) _receiver.DamageDealt += OnImpact;
        }

        private void OnDisable()
        {
            if (_receiver != null) _receiver.DamageDealt -= OnImpact;
        }

        private void OnDestroy()
        {
            // Revert renderers to the original sharedMaterial reference — we
            // never mutate the shared asset so no risk of leaking runtime state
            // to disk (previous approach modified the shared material and
            // could persist a runtime damage-texture reference back to the .mat file).
            foreach (var r in _touchedRenderers)
            {
                if (r == null) continue;
                r.sharedMaterial = _paintMaterial;
            }
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        private void CloneMaterialAndAssign()
        {
            if (_paintMaterial == null)
            {
                Debug.LogError("[PaintDamage] _paintMaterial not assigned.", this);
                enabled = false;
                return;
            }
            var source = _paintMaterial.GetTexture("_BaseMap") as Texture2D ?? _paintMaterial.mainTexture as Texture2D;
            if (source == null)
            {
                Debug.LogError("[PaintDamage] paint material has no source texture.", this);
                enabled = false;
                return;
            }
            if (!source.isReadable)
            {
                Debug.LogError($"[PaintDamage] source texture '{source.name}' is not Read/Write enabled — cannot paint. Enable in importer.", this);
                enabled = false;
                return;
            }

            // Writable clone of the AO texture — we paint into this at runtime.
            _damageTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true);
            _damageTexture.name = source.name + " (Damaged)";
            _damageTexture.wrapMode = source.wrapMode;
            _damageTexture.filterMode = source.filterMode;
            _damageTexture.anisoLevel = source.anisoLevel;
            _originalPixels = source.GetPixels();
            _damageTexture.SetPixels(_originalPixels);
            _damageTexture.Apply(true);

            // Runtime clone of the shared material. We assign THIS to all Paint
            // renderers — the shared asset stays untouched.
            _runtimeMaterial = new Material(_paintMaterial) { name = _paintMaterial.name + " (RuntimeDamage)" };
            _runtimeMaterial.SetTexture("_BaseMap", _damageTexture);
            _runtimeMaterial.mainTexture = _damageTexture;

            // Swap sharedMaterial on every renderer that was using the paint asset
            var allRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                var mats = r.sharedMaterials;
                bool touched = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == _paintMaterial)
                    {
                        mats[i] = _runtimeMaterial;
                        touched = true;
                    }
                }
                if (touched)
                {
                    r.sharedMaterials = mats;
                    _touchedRenderers.Add(r);
                }
            }
            if (_debugLog) Debug.Log($"[PaintDamage] Cloned material for runtime damage; assigned to {_touchedRenderers.Count} renderer(s).", this);
        }

        private void OnImpact(float impulse, Vector3 worldContact, Vector3 worldNormal)
        {
            if (_damageTexture == null || _config == null || _dentables == null) return;
            var worldRadius = _config.DentRadius * _paintRadiusMultiplier;
            var worldRadiusSq = worldRadius * worldRadius;
            int centersPainted = 0;

            foreach (var dm in _dentables)
            {
                if (dm == null) continue;
                if (!string.IsNullOrEmpty(_paintMeshFilter) && !dm.name.Contains(_paintMeshFilter)) continue;
                var mf = dm.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                if (!mesh.isReadable) continue;

                var localContact = dm.transform.InverseTransformPoint(worldContact);
                var verts = mesh.vertices;
                var uvs = mesh.uv;
                if (uvs == null || uvs.Length != verts.Length) continue;

                // Collect ALL verts within the world radius so we cover every UV
                // island the impact spans (a hit near a panel seam maps to multiple
                // disjoint atlas regions — we must paint into each of them).
                var inRange = new System.Collections.Generic.List<int>();
                for (int i = 0; i < verts.Length; i++)
                {
                    if ((verts[i] - localContact).sqrMagnitude <= worldRadiusSq) inRange.Add(i);
                }
                if (inRange.Count == 0) continue;

                // Sample up to _maxClustersPerPanel evenly-spaced verts from the
                // in-range set. Each becomes an independent scratch cluster in
                // texture space. This bounds cost while covering UV seams.
                int clusterCount = Mathf.Min(_maxClustersPerPanel, inRange.Count);
                int step = Mathf.Max(1, inRange.Count / clusterCount);
                for (int k = 0; k < inRange.Count; k += step)
                {
                    PaintScratchCluster(uvs[inRange[k]]);
                    centersPainted++;
                }
            }

            if (centersPainted > 0)
            {
                // Apply(true) rebuilds mipmaps so damage stays visible at any
                // camera distance / view angle.
                _damageTexture.Apply(true);
                if (_debugLog) Debug.Log($"[PaintDamage] painted {centersPainted} scratch cluster(s) (impulse={impulse:F1})", this);
            }
        }

        private void PaintScratchCluster(Vector2 uv)
        {
            int cx = Mathf.RoundToInt(uv.x * _damageTexture.width);
            int cy = Mathf.RoundToInt(uv.y * _damageTexture.height);

            int scratchCount = Random.Range(_scratchesMin, _scratchesMax + 1);
            for (int i = 0; i < scratchCount; i++)
            {
                // Random start point inside the scatter circle
                var offset = Random.insideUnitCircle * _scatterRadius;
                int sx = cx + Mathf.RoundToInt(offset.x);
                int sy = cy + Mathf.RoundToInt(offset.y);

                bool isLong = Random.value < _longScratchFraction;
                int length = isLong
                    ? Random.Range(_longLenMin, _longLenMax + 1)
                    : Random.Range(_shortLenMin, _shortLenMax + 1);

                // Fully random direction — no radial bias, so patterns look scattered
                float angle = Random.Range(0f, Mathf.PI * 2f);
                int ex = sx + Mathf.RoundToInt(Mathf.Cos(angle) * length);
                int ey = sy + Mathf.RoundToInt(Mathf.Sin(angle) * length);

                float intensity = _brushIntensity * Random.Range(0.75f, 1f);
                DrawLine(sx, sy, ex, ey, intensity);
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, float intensity)
        {
            // Bresenham line rasterizer with 1–2 px thickness that tapers
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int steps = 0;
            int totalSteps = Mathf.Max(dx, dy);
            while (true)
            {
                // Very gentle fade toward the tip so tip pixels stay clearly visible
                float t = totalSteps > 0 ? 1f - (steps / (float)totalSteps) : 1f;
                float alpha = intensity * (0.75f + 0.25f * t);
                // Sharp single-pixel core; the two diagonal neighbors get a tiny
                // wash for anti-alias only (no perceived thickening).
                StampPixel(x0, y0, alpha);
                StampPixel(x0 + 1, y0 + 1, alpha * 0.3f);
                StampPixel(x0 - 1, y0 - 1, alpha * 0.3f);
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
                steps++;
                if (steps > totalSteps + 1) break;
            }
        }

        private void StampPixel(int px, int py, float alpha)
        {
            if (px < 0 || py < 0 || px >= _damageTexture.width || py >= _damageTexture.height) return;
            if (alpha <= 0f) return;
            var current = _damageTexture.GetPixel(px, py);
            _damageTexture.SetPixel(px, py, Color.Lerp(current, _damageColor, Mathf.Clamp01(alpha)));
        }

        /// Restore the original paint pixels — pairs with DentableMesh.ResetToOriginal.
        public void ResetPaint()
        {
            if (_damageTexture == null || _originalPixels == null) return;
            _damageTexture.SetPixels(_originalPixels);
            _damageTexture.Apply(false);
            if (_debugLog) Debug.Log("[PaintDamage] Paint texture reset to original.", this);
        }
    }
}
