using System.Collections.Generic;
using UnityEngine;

namespace DriftAssignment.Damage
{
    /// Keeps damage feel coherent across the whole car:
    ///  - Temporal sync: as CarHealth drops past named thresholds, progressively
    ///    hide/shatter groups of parts (lights, glass) even if their local hit
    ///    thresholds were never crossed.
    ///  - Spatial sync: on every ImpactReceiver hit, propagate breakage to
    ///    glass/light components within a radius of the contact point so a
    ///    heavy rear collision cracks the nearby tail light even without a
    ///    direct hit.
    [DisallowMultipleComponent]
    public class DamageCascade : MonoBehaviour
    {
        [System.Serializable]
        public class HealthStage
        {
            public string Name;
            [Range(0f, 1f)] public float DamageFraction = 0.5f;
            public GameObject[] TargetsToHide;
            [HideInInspector] public bool Fired;
        }

        [SerializeField] private CarHealth _health;
        [SerializeField] private ImpactReceiver _receiver;

        [Header("Progressive cascade by total damage")]
        [SerializeField] private List<HealthStage> _stages = new List<HealthStage>();

        [Header("Spatial propagation per impact")]
        [Tooltip("Impacts within this radius of a glass/light component break it if impulse is high enough")]
        [SerializeField] private float _propagationRadius = 1.2f;
        [Tooltip("Impulse multiplier applied when checking propagated hits — >1 means propagation is easier to trigger than direct impact")]
        [SerializeField] private float _propagationImpulseBoost = 1.5f;

        [SerializeField] private bool _debugLog = false;

        private GlassShatter[] _allGlass;

        private void Awake()
        {
            if (_health == null) _health = GetComponent<CarHealth>();
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
            RefreshCache();
        }

        public void RefreshCache()
        {
            _allGlass = GetComponentsInChildren<GlassShatter>(true);
        }

        private void OnEnable()
        {
            if (_health != null) _health.HealthChanged += OnHealthChanged;
            if (_receiver != null) _receiver.DamageDealt += OnImpact;
        }

        private void OnDisable()
        {
            if (_health != null) _health.HealthChanged -= OnHealthChanged;
            if (_receiver != null) _receiver.DamageDealt -= OnImpact;
        }

        private void OnHealthChanged(float healthNormalized)
        {
            var dmg = 1f - healthNormalized;
            foreach (var stage in _stages)
            {
                if (stage.Fired) continue;
                if (dmg < stage.DamageFraction) continue;
                Fire(stage);
            }
        }

        private void Fire(HealthStage stage)
        {
            stage.Fired = true;
            int hidden = 0;
            if (stage.TargetsToHide != null)
            {
                foreach (var t in stage.TargetsToHide)
                {
                    if (t == null || !t.activeSelf) continue;
                    var gs = t.GetComponent<GlassShatter>();
                    if (gs != null) gs.Shatter($"cascade:{stage.Name}");
                    else t.SetActive(false);
                    hidden++;
                }
            }
            if (_debugLog) Debug.Log($"[DamageCascade] Stage '{stage.Name}' fired at dmg≥{stage.DamageFraction:P0} → hid {hidden} parts", this);
        }

        private void OnImpact(float impulse, Vector3 worldContact, Vector3 worldNormal)
        {
            if (_allGlass == null || _allGlass.Length == 0) return;
            var effective = impulse * _propagationImpulseBoost;
            var r2 = _propagationRadius * _propagationRadius;
            int cracked = 0;
            foreach (var g in _allGlass)
            {
                if (g == null || !g.isActiveAndEnabled) continue;
                var sqrDist = (g.transform.position - worldContact).sqrMagnitude;
                if (sqrDist > r2) continue;
                // Ask the glass component to try shattering as if it had received this impulse.
                g.ReceiveImpact(worldContact, Vector3.zero, effective);
                cracked++;
            }
            if (_debugLog && cracked > 0) Debug.Log($"[DamageCascade] Spatial propagation: {cracked} glass within {_propagationRadius}m of hit", this);
        }

        /// Called by DamageTestPanel.ResetAll — resets fired flags too.
        public void ResetCascade()
        {
            foreach (var s in _stages) s.Fired = false;
        }
    }
}
