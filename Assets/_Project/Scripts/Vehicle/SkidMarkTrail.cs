using UnityEngine;

namespace DriftAssignment.Vehicle
{
    /// One per wheel. Samples the WheelCollider's ground contact each frame,
    /// enables a TrailRenderer (skid mark) + ParticleSystem (tire smoke) when
    /// slip crosses a threshold, and rides the ground surface so the trail
    /// stays glued to the road.
    [DisallowMultipleComponent]
    public class SkidMarkTrail : MonoBehaviour
    {
        [SerializeField] private WheelCollider _wheel;

        [Header("Slip thresholds (WheelHit sideways slip)")]
        [Tooltip("Absolute sideways slip above which the trail + smoke turn on")]
        [SerializeField] private float _sidewaysSlipThreshold = 0.4f;
        [Tooltip("Absolute forward slip (throttle/brake burnout) above which the trail + smoke turn on")]
        [SerializeField] private float _forwardSlipThreshold = 0.6f;

        [Header("Trail")]
        [SerializeField] private float _trailWidth = 0.28f;
        [SerializeField] private float _trailLifetime = 8f;
        [SerializeField] private Color _trailColor = new Color(0.08f, 0.07f, 0.07f, 0.85f);
        [Tooltip("How high above the ground contact the trail sits (avoid z-fight)")]
        [SerializeField] private float _trailGroundOffset = 0.02f;

        [Header("Tire smoke")]
        [Tooltip("If set, this prefab (e.g. smoke_thick.prefab from the effect pack) is instantiated as a child and its emission scales with slip. Overrides the code-built puff.")]
        [SerializeField] private GameObject _smokePrefab;
        [SerializeField] private Material _smokeMaterial;
        [SerializeField] private float _smokeRate = 30f;
        [SerializeField] private float _smokeLifetime = 1.4f;
        [SerializeField] private Color _smokeColor = new Color(0.35f, 0.32f, 0.3f, 0.6f);

        [Header("Prefab smoke overrides — keep smoke low + short-lived")]
        [Tooltip("Force the prefab's max particle lifetime to this value (seconds)")]
        [SerializeField] private float _prefabSmokeLifetime = 0.7f;
        [Tooltip("Scale down the prefab's start-speed (upward drift)")]
        [SerializeField] private float _prefabSmokeSpeedScale = 0.5f;
        [Tooltip("Scale down the prefab's start-size so the puff is tire-height, not car-height")]
        [SerializeField] private float _prefabSmokeSizeScale = 0.6f;
        [Tooltip("Extra gravity applied to the smoke so it hugs the ground and dies before rising past the car")]
        [SerializeField] private float _prefabSmokeGravity = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private TrailRenderer _trail;
        private ParticleSystem _smoke;
        private static Material s_trailMaterial;

        private void Awake()
        {
            if (_wheel == null) _wheel = GetComponentInParent<WheelCollider>();
            BuildTrail();
            BuildSmoke();
        }

        private void BuildTrail()
        {
            EnsureTrailMaterial();
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.sharedMaterial = s_trailMaterial;
            _trail.time = _trailLifetime;
            _trail.startWidth = _trailWidth;
            _trail.endWidth = _trailWidth * 0.8f;
            _trail.emitting = false;
            _trail.minVertexDistance = 0.05f;
            _trail.autodestruct = false;
            _trail.receiveShadows = false;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(_trailColor, 0f), new GradientColorKey(_trailColor * 0.7f, 1f) },
                new[] { new GradientAlphaKey(_trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = grad;
        }

        private static void EnsureTrailMaterial()
        {
            if (s_trailMaterial != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            s_trailMaterial = new Material(shader) { name = "URP.SkidTrail" };
            s_trailMaterial.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            s_trailMaterial.SetFloat("_Surface", 1f);
            s_trailMaterial.SetFloat("_Blend", 0f);
            s_trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_trailMaterial.SetInt("_ZWrite", 0);
            s_trailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_trailMaterial.renderQueue = 3005;
        }

        private void BuildSmoke()
        {
            // Prefer the artist-authored prefab from the imported effect pack
            if (_smokePrefab != null)
            {
                var inst = Instantiate(_smokePrefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.name = "TireSmoke(prefab)";
                _smoke = inst.GetComponent<ParticleSystem>();
                if (_smoke == null) _smoke = inst.GetComponentInChildren<ParticleSystem>(true);
                if (_smoke == null)
                {
                    if (_debugLog) Debug.LogWarning("[SkidMarkTrail] smoke prefab has no ParticleSystem — falling back to procedural", this);
                }
                else
                {
                    // Emission starts at 0 — Update() drives it based on slip
                    var em = _smoke.emission;
                    em.rateOverTime = 0f;
                    // Force short lifetime, low upward speed, small size + gravity
                    // so tire smoke never rises past the car body.
                    _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var prefabMain = _smoke.main;
                    prefabMain.startLifetime = _prefabSmokeLifetime;
                    prefabMain.startSpeedMultiplier = _prefabSmokeSpeedScale;
                    prefabMain.startSizeMultiplier = _prefabSmokeSizeScale;
                    prefabMain.gravityModifier = _prefabSmokeGravity;
                    if (!_smoke.isPlaying) _smoke.Play();
                    return;
                }
            }

            var smokeGo = new GameObject("TireSmoke");
            smokeGo.transform.SetParent(transform, false);
            _smoke = smokeGo.AddComponent<ParticleSystem>();
            var psr = smokeGo.GetComponent<ParticleSystemRenderer>();
            if (_smokeMaterial != null) psr.sharedMaterial = _smokeMaterial;

            var main = _smoke.main;
            main.loop = true;
            main.startLifetime = _smokeLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = _smokeColor;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            var emission = _smoke.emission;
            emission.rateOverTime = 0f;

            var shape = _smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var vel = _smoke.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);

            var col = _smoke.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(_smokeColor * 1.1f, 0f), new GradientColorKey(_smokeColor * 0.7f, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(_smokeColor.a, 0.15f),
                    new GradientAlphaKey(0f, 1f),
                });
            col.color = grad;

            var size = _smoke.sizeOverLifetime;
            size.enabled = true;
            var sc = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1.8f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sc);

            _smoke.Play();
        }

        private void Update()
        {
            if (_wheel == null) return;

            bool grounded = _wheel.GetGroundHit(out var hit);
            bool slipping = false;
            float slipAmount = 0f;
            if (grounded)
            {
                var lat = Mathf.Abs(hit.sidewaysSlip);
                var lon = Mathf.Abs(hit.forwardSlip);
                slipping = lat > _sidewaysSlipThreshold || lon > _forwardSlipThreshold;
                slipAmount = Mathf.Max(lat, lon);
            }

            // Ride the ground contact so the trail sits on the road
            if (grounded)
            {
                transform.position = hit.point + hit.normal * _trailGroundOffset;
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, transform.right), hit.normal);
            }
            else
            {
                // Off ground → snap trail up under wheel to keep segment continuous
                transform.position = _wheel.transform.position - Vector3.up * _wheel.radius;
            }

            if (_trail != null) _trail.emitting = slipping && grounded;
            if (_smoke != null)
            {
                var em = _smoke.emission;
                em.rateOverTime = (slipping && grounded) ? _smokeRate * Mathf.Clamp01(slipAmount) : 0f;
            }

            if (_debugLog && slipping) Debug.Log($"[SkidMarkTrail] {name} slip={slipAmount:F2}", this);
        }
    }
}
