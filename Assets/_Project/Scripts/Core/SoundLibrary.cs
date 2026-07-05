using UnityEngine;

namespace DriftAssignment.Core
{
    /// Central audio config. Any component that plays sound references this SO
    /// via [SerializeField] and pulls clips by named field — no inline AudioClips.
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Drift/Sound Library", order = 20)]
    public class SoundLibrary : ScriptableObject
    {
        [Header("Engine (long-form car recordings)")]
        public AudioClip EngineSlow;
        public AudioClip EngineIdle;
        public AudioClip EngineFast;
        public AudioClip HandBrakeInterior;

        [Header("Drift / Rally (drift-feel + races)")]
        public AudioClip DriftBrakingCornering;
        public AudioClip RaceStart;
        public AudioClip PassBy;
        public AudioClip UphillPassBy;

        [Header("Impact — Metal")]
        public AudioClip[] MetalLight;
        public AudioClip[] MetalMedium;
        public AudioClip[] MetalHeavy;

        [Header("Impact — Plate (panel)")]
        public AudioClip[] PlateLight;
        public AudioClip[] PlateMedium;
        public AudioClip[] PlateHeavy;

        [Header("Impact — Glass")]
        public AudioClip[] GlassLight;
        public AudioClip[] GlassMedium;
        public AudioClip[] GlassHeavy;
        public AudioClip GlassShatterBig;

        [Header("Damage — Heavy / Debris")]
        public AudioClip CarExplosion;
        public AudioClip MetalCrashDebris;
        public AudioClip MetalScreech;
        public AudioClip MetalImpact;
        public AudioClip MetalReversed;

        [Header("Impact — Generic")]
        public AudioClip[] GenericLight;

        [Header("Character (stretch — Phase 10)")]
        public AudioClip[] FootstepConcrete;

        [Header("UI")]
        public AudioClip UIClick;
        public AudioClip UICounter;
        public AudioClip UIFeedback;
        public AudioClip UIZoom;

        public AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
