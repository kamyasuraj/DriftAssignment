using UnityEngine;

namespace DriftAssignment.Damage
{
    [CreateAssetMenu(fileName = "DamageConfig", menuName = "Drift/Damage Config", order = 30)]
    public class DamageConfig : ScriptableObject
    {
        [Header("Dent Stage")]
        [Tooltip("Minimum impulse magnitude to register a dent")]
        public float MinDentImpulse = 1.5f;
        [Tooltip("Radius (car-local units) around contact where verts displace")]
        public float DentRadius = 0.35f;
        [Tooltip("Max depth in local units — dent depth is scaled by impulse but clamped here")]
        public float DentMaxDepth = 0.15f;
        [Tooltip("Falloff sharpness: 1=linear cone, 2=quadratic (default)")]
        public float DentFalloff = 2f;
        [Tooltip("Impulse → depth scale factor before clamp")]
        public float DentImpulseScale = 0.005f;

        [Header("Break Stage")]
        [Tooltip("Accumulated impact required for a part to detach")]
        public float BreakThreshold = 25f;
        [Tooltip("Rigidbody mass applied to a detached part")]
        public float DetachedPartMass = 20f;
        [Tooltip("Outward impulse scale on detach")]
        public float DetachImpulseScale = 0.4f;
        [Tooltip("Seconds a detached part exists before despawn")]
        public float DetachedLifetime = 8f;

        [Header("Glass")]
        [Tooltip("Impulse magnitude to shatter a glass panel")]
        public float GlassShatterThreshold = 8f;

        [Header("Debug")]
        public bool DebugLog = false;
    }
}
