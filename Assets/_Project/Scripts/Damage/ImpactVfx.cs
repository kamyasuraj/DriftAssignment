using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Listens to ImpactReceiver.DamageDealt. On each qualifying hit spawns:
    ///  - a spark burst — additive-blended, per-particle trails, glow-like
    ///  - a dust puff — grey, spreads outward, alpha-fades
    /// Paint spoilage is handled by PaintDamage (writes into the material's
    /// albedo texture at impact UVs); this component focuses on airborne VFX.
    [DisallowMultipleComponent]
    public class ImpactVfx : MonoBehaviour
    {
        [SerializeField] private ImpactReceiver _receiver;
        [SerializeField] private Transform _carRoot;

        [Header("Impulse thresholds")]
        [SerializeField] private float _minImpulseForVfx = 8f;
        [SerializeField] private float _heavyImpulseThreshold = 300f;

        [Header("Sparks (grinder-style streaks)")]
        [SerializeField] private int _sparkBaseCount = 60;
        [SerializeField] private int _sparkIntensityCount = 220;
        [SerializeField] private float _sparkSpeedMin = 8f;
        [SerializeField] private float _sparkSpeedMax = 28f;
        [SerializeField] private float _sparkSize = 0.045f;
        [SerializeField] private float _sparkLifetimeMin = 0.4f;
        [SerializeField] private float _sparkLifetimeMax = 1.1f;
        [SerializeField] private float _sparkStretchLength = 3.4f;
        [SerializeField] private float _sparkConeAngle = 32f;

        [Header("Dust")]
        [SerializeField] private int _dustBaseCount = 6;
        [SerializeField] private int _dustIntensityCount = 24;

        [Header("Optional imported assets")]
        [Tooltip("If assigned, this prefab is instantiated at contact instead of the procedural spark system (e.g. Spark.prefab from effect pack)")]
        [SerializeField] private GameObject _sparkPrefab;
        [SerializeField] private float _sparkPrefabLifetime = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private static Material s_sparkMaterial;

        private void Awake()
        {
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
            if (_carRoot == null) _carRoot = transform;
            EnsureSparkMaterial();
        }

        private void OnEnable()
        {
            if (_receiver != null) _receiver.DamageDealt += OnImpact;
        }

        private void OnDisable()
        {
            if (_receiver != null) _receiver.DamageDealt -= OnImpact;
        }

        private static void EnsureSparkMaterial()
        {
            if (s_sparkMaterial != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            s_sparkMaterial = new Material(shader) { name = "URP.Particle.SparkAdditive" };
            s_sparkMaterial.color = new Color(1.5f, 1.2f, 0.4f, 1f); // HDR-boosted for bloom-friendliness
            s_sparkMaterial.mainTexture = BuildSparkTexture(64);
            s_sparkMaterial.SetTexture("_BaseMap", s_sparkMaterial.mainTexture);
            s_sparkMaterial.SetFloat("_Surface", 1f);
            s_sparkMaterial.SetFloat("_Blend", 1f); // 1 = additive on URP Particles Unlit
            s_sparkMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_sparkMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive
            s_sparkMaterial.SetInt("_ZWrite", 0);
            s_sparkMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_sparkMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            s_sparkMaterial.renderQueue = 3050;
        }

        private static Texture2D BuildSparkTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var maxDist = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var dx = (x - center) / maxDist;
                    var dy = (y - center) / maxDist;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(1f - d);
                    a = a * a * a; // hot core, soft halo
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }

        private void OnImpact(float impulse, Vector3 worldContact, Vector3 worldNormal)
        {
            if (impulse < _minImpulseForVfx) return;
            var n = Mathf.InverseLerp(_minImpulseForVfx, _heavyImpulseThreshold, impulse);
            SpawnSparks(worldContact, worldNormal, n);
            SpawnDust(worldContact, n);
            if (_debugLog) Debug.Log($"[ImpactVfx] impulse={impulse:F1} n={n:F2}", this);
        }

        private void SpawnSparks(Vector3 world, Vector3 normal, float intensity)
        {
            if (_sparkPrefab != null)
            {
                var rot = Quaternion.LookRotation(normal.sqrMagnitude > 0.01f ? normal : Vector3.up);
                var inst = Instantiate(_sparkPrefab, world, rot);
                inst.name = "ImpactSparks(prefab)";
                var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var p in pss)
                {
                    var m = p.main;
                    // Scale the burst by intensity so light taps don't fire welder-torch sparks
                    m.startSpeedMultiplier = 0.8f + intensity * 1.4f;
                    m.startSizeMultiplier = 0.7f + intensity * 0.9f;
                    p.Play(true);
                }
                Destroy(inst, _sparkPrefabLifetime);
                return;
            }
            var go = new GameObject("ImpactSparks");
            go.transform.position = world;
            go.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.01f ? normal : Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = s_sparkMaterial;
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.lengthScale = _sparkStretchLength;
            psr.velocityScale = 0.12f;

            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_sparkLifetimeMin, _sparkLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_sparkSpeedMin, _sparkSpeedMax * (0.7f + intensity * 0.6f));
            main.startSize = new ParticleSystem.MinMaxCurve(_sparkSize * 0.5f, _sparkSize * (1.1f + intensity * 0.5f));
            main.startColor = new Color(1f, 0.9f, 0.4f, 1f);
            main.gravityModifier = 1.4f;
            main.maxParticles = 600;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burstCount = (short)(_sparkBaseCount + intensity * _sparkIntensityCount);
            emission.SetBurst(0, new ParticleSystem.Burst(0f, burstCount));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _sparkConeAngle;
            shape.radius = 0.02f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.55f),
                    new GradientColorKey(new Color(0.35f, 0.12f, 0.03f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sc = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sc);

            ps.Play(true);
            Destroy(go, 3f);
        }

        private void SpawnDust(Vector3 world, float intensity)
        {
            var dustGo = new GameObject("ImpactDust");
            dustGo.transform.position = world;
            var dust = dustGo.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            dem.SetBurst(0, new ParticleSystem.Burst(0f, (short)(_dustBaseCount + intensity * _dustIntensityCount)));
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

            dust.Play(true);
            Destroy(dustGo, 2.5f);
        }
    }
}
