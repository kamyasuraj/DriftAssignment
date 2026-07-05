using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Listens to ImpactReceiver.DamageDealt. On each qualifying hit spawns:
    ///  - a spark burst (bright, short-lived)
    ///  - a dust puff (grey, spreads, fades)
    ///  - a scratch decal quad projected onto the nearest panel and parented
    ///    to the car so it moves with damage.
    /// All resources are generated at runtime — no prefab dependencies.
    [DisallowMultipleComponent]
    public class ImpactVfx : MonoBehaviour
    {
        [SerializeField] private ImpactReceiver _receiver;
        [SerializeField] private Transform _carRoot;

        [Header("Impulse thresholds")]
        [SerializeField] private float _minImpulseForVfx = 8f;
        [SerializeField] private float _heavyImpulseThreshold = 300f;

        [Header("Scratch decal")]
        [SerializeField] private float _decalMinSize = 0.15f;
        [SerializeField] private float _decalMaxSize = 0.55f;
        [SerializeField] private float _decalLifetime = 30f;
        [SerializeField] private int _maxLiveDecals = 40;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Material _scratchMaterial;
        private Mesh _quadMesh;
        private System.Collections.Generic.Queue<GameObject> _decalPool = new System.Collections.Generic.Queue<GameObject>();

        private void Awake()
        {
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
            if (_carRoot == null) _carRoot = transform;
            BuildScratchAssets();
        }

        private void OnEnable()
        {
            if (_receiver != null) _receiver.DamageDealt += OnImpact;
        }

        private void OnDisable()
        {
            if (_receiver != null) _receiver.DamageDealt -= OnImpact;
        }

        private void BuildScratchAssets()
        {
            _quadMesh = new Mesh { name = "ImpactVfx.Quad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            };
            _quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quadMesh.RecalculateNormals();

            var tex = GenerateScratchTexture(128);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            _scratchMaterial = new Material(shader);
            _scratchMaterial.mainTexture = tex;
            _scratchMaterial.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            _scratchMaterial.SetFloat("_Surface", 1f); // transparent for URP Unlit
            _scratchMaterial.SetFloat("_Blend", 0f);
            _scratchMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _scratchMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _scratchMaterial.SetInt("_ZWrite", 0);
            _scratchMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _scratchMaterial.renderQueue = 3000;
        }

        private Texture2D GenerateScratchTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var maxDist = size * 0.5f;

            // Radial fade + noisy scratches
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    var alpha = Mathf.Clamp01(1f - d);
                    alpha *= alpha; // sharpen
                    alpha *= Random.Range(0.4f, 1f); // grainy
                    pixels[y * size + x] = new Color32(20, 15, 15, (byte)(alpha * 255));
                }
            }
            // Add streaks
            for (int s = 0; s < 6; s++)
            {
                var start = new Vector2(Random.Range(0, size), Random.Range(0, size));
                var dir = Random.insideUnitCircle.normalized * Random.Range(size * 0.15f, size * 0.35f);
                int steps = 40;
                for (int i = 0; i < steps; i++)
                {
                    var t = i / (float)steps;
                    var p = start + dir * t;
                    int px = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, size - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(p.y), 0, size - 1);
                    pixels[py * size + px] = new Color32(10, 8, 8, 255);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private void OnImpact(float impulse, Vector3 worldContact)
        {
            if (impulse < _minImpulseForVfx) return;

            var normalized = Mathf.InverseLerp(_minImpulseForVfx, _heavyImpulseThreshold, impulse);
            SpawnBurst(worldContact, normalized);
            SpawnScratch(worldContact, normalized);
            if (_debugLog) Debug.Log($"[ImpactVfx] impact impulse={impulse:F1} normalized={normalized:F2}", this);
        }

        private void SpawnBurst(Vector3 world, float intensity)
        {
            var go = new GameObject("ImpactBurst");
            go.transform.position = world;
            var ps = go.AddComponent<ParticleSystem>();
            DamageSmoke.AssignUrpParticleMaterial(go.GetComponent<ParticleSystemRenderer>());
            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = 0.5f + intensity * 0.8f;
            main.startSpeed = 3f + intensity * 6f;
            main.startSize = 0.03f + intensity * 0.08f;
            main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
            main.gravityModifier = 0.6f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)(6 + intensity * 40)));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f), new GradientColorKey(new Color(0.4f, 0.2f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            // dust puff — second particle system on same GO
            var dustGo = new GameObject("ImpactDust");
            dustGo.transform.SetParent(go.transform, false);
            var dust = dustGo.AddComponent<ParticleSystem>();
            DamageSmoke.AssignUrpParticleMaterial(dustGo.GetComponent<ParticleSystemRenderer>());
            var dmain = dust.main;
            dmain.duration = 0.4f;
            dmain.loop = false;
            dmain.startLifetime = 0.8f + intensity * 0.6f;
            dmain.startSpeed = 1f + intensity * 3f;
            dmain.startSize = 0.15f + intensity * 0.4f;
            dmain.startColor = new Color(0.55f, 0.5f, 0.45f, 0.55f);
            dmain.gravityModifier = -0.1f;
            dmain.stopAction = ParticleSystemStopAction.Destroy;
            var dem = dust.emission;
            dem.rateOverTime = 0f;
            dem.SetBurst(0, new ParticleSystem.Burst(0f, (short)(4 + intensity * 20)));
            var dshape = dust.shape;
            dshape.shapeType = ParticleSystemShapeType.Sphere;
            dshape.radius = 0.1f;
            var dcol = dust.colorOverLifetime;
            dcol.enabled = true;
            var dgrad = new Gradient();
            dgrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 0f), new GradientColorKey(new Color(0.4f, 0.35f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
            dcol.color = dgrad;

            Destroy(go, 2f);
        }

        private void SpawnScratch(Vector3 world, float intensity)
        {
            if (_decalPool.Count >= _maxLiveDecals)
            {
                var oldest = _decalPool.Dequeue();
                if (oldest != null) Destroy(oldest);
            }

            var go = new GameObject("Scratch");
            go.transform.position = world;
            // Face outward (away from car center) with random spin
            var toCar = (_carRoot.position - world).normalized;
            go.transform.rotation = Quaternion.LookRotation(toCar) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            // Nudge slightly outward so it doesn't z-fight
            go.transform.position += -toCar * 0.005f;
            go.transform.SetParent(_carRoot, true);

            var size = Mathf.Lerp(_decalMinSize, _decalMaxSize, intensity) * Random.Range(0.7f, 1.3f);
            go.transform.localScale = new Vector3(size, size, size);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _quadMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _scratchMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            _decalPool.Enqueue(go);
            if (_decalLifetime > 0f) Destroy(go, _decalLifetime);
        }
    }
}
