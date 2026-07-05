using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Persistent smoke emitter that ramps with accumulated damage. Attach to
    /// any point on the car (hood, engine bay, exhaust). Auto-configures a
    /// ParticleSystem on the same GameObject if none is present.
    [DisallowMultipleComponent]
    public class DamageSmoke : MonoBehaviour
    {
        [SerializeField] private CarHealth _health;
        [Tooltip("Damage fraction at which smoke starts (0..1)")]
        [Range(0f, 1f)] [SerializeField] private float _startAtDamage = 0.35f;
        [Tooltip("Damage fraction at which smoke is fully thick (0..1)")]
        [Range(0f, 1f)] [SerializeField] private float _maxAtDamage = 0.9f;

        [Header("Emission")]
        [SerializeField] private float _maxEmissionRate = 45f;
        [SerializeField] private float _startLifetime = 2.5f;
        [SerializeField] private float _startSpeed = 1.3f;
        [SerializeField] private float _startSizeMin = 0.35f;
        [SerializeField] private float _startSizeMax = 0.9f;
        [SerializeField] private Color _startColor = new Color(0.35f, 0.32f, 0.3f, 0.65f);
        [SerializeField] private Vector3 _localVelocity = new Vector3(0f, 1.4f, 0f);

        private ParticleSystem _ps;
        private static Material s_sharedParticleMaterial;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = gameObject.AddComponent<ParticleSystem>();
            ConfigureParticleSystem();
            AssignUrpParticleMaterial(GetComponent<ParticleSystemRenderer>());
            if (_health == null) _health = GetComponentInParent<CarHealth>();
        }

        public static void AssignUrpParticleMaterial(ParticleSystemRenderer renderer)
        {
            if (renderer == null) return;
            if (s_sharedParticleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                s_sharedParticleMaterial = new Material(shader) { name = "URP.Particle.Smoke" };
                s_sharedParticleMaterial.color = Color.white;
                s_sharedParticleMaterial.mainTexture = GenerateSoftPuffTexture(128);
                s_sharedParticleMaterial.SetTexture("_BaseMap", s_sharedParticleMaterial.mainTexture);
                s_sharedParticleMaterial.SetFloat("_Surface", 1f);
                s_sharedParticleMaterial.SetFloat("_Blend", 0f);
                s_sharedParticleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_sharedParticleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_sharedParticleMaterial.SetInt("_ZWrite", 0);
                s_sharedParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                s_sharedParticleMaterial.renderQueue = 3000;
            }
            renderer.sharedMaterial = s_sharedParticleMaterial;
        }

        private static Texture2D GenerateSoftPuffTexture(int size)
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
                    // Soft cosine falloff for a natural puff shape
                    var alpha = Mathf.Clamp01(1f - d);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    alpha *= alpha; // sharpen center
                    // Add subtle noise so puffs don't look perfectly uniform
                    alpha *= (0.85f + Random.value * 0.15f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }

        private void ConfigureParticleSystem()
        {
            var main = _ps.main;
            main.loop = true;
            main.startLifetime = _startLifetime;
            main.startSpeed = _startSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(_startSizeMin, _startSizeMax);
            main.startColor = _startColor;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = _ps.emission;
            emission.rateOverTime = 0f;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var vel = _ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(_localVelocity.x - 0.2f, _localVelocity.x + 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(_localVelocity.y - 0.3f, _localVelocity.y + 0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(_localVelocity.z - 0.2f, _localVelocity.z + 0.2f);

            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(_startColor * 1.1f, 0f),
                    new GradientColorKey(_startColor * 0.75f, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(_startColor.a, 0.2f),
                    new GradientAlphaKey(0f, 1f),
                });
            col.color = grad;

            var size = _ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1.6f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Ensure renderer uses a soft material — default is fine on URP.
        }

        private void Update()
        {
            if (_health == null || _ps == null) return;
            var dmg = 1f - _health.HealthNormalized;
            var t = Mathf.InverseLerp(_startAtDamage, _maxAtDamage, dmg);
            var rate = t * _maxEmissionRate;
            var em = _ps.emission;
            em.rateOverTime = rate;
        }
    }
}
