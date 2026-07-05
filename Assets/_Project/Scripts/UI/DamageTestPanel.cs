using DriftAssignment.Damage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.UI
{
    /// Runtime damage tuning overlay. Live sliders bound to DamageConfig fields,
    /// buttons that fire synthetic dents at pre-defined car-local regions, and a
    /// reset that restores all body panels to their original mesh state.
    /// Toggle with F1.
    [DisallowMultipleComponent]
    public class DamageTestPanel : MonoBehaviour
    {
        [SerializeField] private DamageConfig _config;
        [SerializeField] private ImpactReceiver _receiver;
        [SerializeField] private Transform _carRoot;
        [SerializeField] private Key _toggleKey = Key.F1;
        [SerializeField] private bool _visible = true;
        [SerializeField] private int _panelWidth = 680;
        [SerializeField] private int _panelHeight = 1100;
        [SerializeField] private int _fontSize = 18;

        [Header("Region hit points (car-local)")]
        [SerializeField] private Vector3 _hitFront = new Vector3(0f, 0.8f, 2.3f);
        [SerializeField] private Vector3 _hitRear = new Vector3(0f, 0.8f, -2.3f);
        [SerializeField] private Vector3 _hitHood = new Vector3(0f, 1.1f, 1.4f);
        [SerializeField] private Vector3 _hitRoof = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private Vector3 _hitLeftDoor = new Vector3(-0.9f, 0.9f, 0.3f);
        [SerializeField] private Vector3 _hitRightDoor = new Vector3(0.9f, 0.9f, 0.3f);
        [SerializeField] private Vector3 _hitLeftFender = new Vector3(-0.9f, 0.8f, 1.6f);
        [SerializeField] private Vector3 _hitRightFender = new Vector3(0.9f, 0.8f, 1.6f);

        [Header("Simulated impact")]
        [SerializeField] private float _simulatedImpulse = 300f;

        private DentableMesh[] _dentables;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;
        private Vector2 _scroll;

        // Snapshot of DamageConfig at Awake so "Reset Params" restores to
        // whatever we started Play with (the SO's authored defaults).
        private DamageDefaults _defaults;

        private struct DamageDefaults
        {
            public float MinDentImpulse, DentRadius, DentMaxDepth, DentFalloff, DentImpulseScale;
            public float BreakThreshold, DetachedPartMass, DetachImpulseScale, DetachedLifetime;
            public float GlassShatterThreshold;
        }

        private void Awake()
        {
            if (_carRoot == null) _carRoot = transform;
            if (_receiver == null) _receiver = GetComponent<ImpactReceiver>();
            RefreshCache();
            SnapshotDefaults();
        }

        private void SnapshotDefaults()
        {
            if (_config == null) return;
            _defaults = new DamageDefaults
            {
                MinDentImpulse = _config.MinDentImpulse,
                DentRadius = _config.DentRadius,
                DentMaxDepth = _config.DentMaxDepth,
                DentFalloff = _config.DentFalloff,
                DentImpulseScale = _config.DentImpulseScale,
                BreakThreshold = _config.BreakThreshold,
                DetachedPartMass = _config.DetachedPartMass,
                DetachImpulseScale = _config.DetachImpulseScale,
                DetachedLifetime = _config.DetachedLifetime,
                GlassShatterThreshold = _config.GlassShatterThreshold,
            };
        }

        private void RestoreDefaults()
        {
            if (_config == null) return;
            _config.MinDentImpulse = _defaults.MinDentImpulse;
            _config.DentRadius = _defaults.DentRadius;
            _config.DentMaxDepth = _defaults.DentMaxDepth;
            _config.DentFalloff = _defaults.DentFalloff;
            _config.DentImpulseScale = _defaults.DentImpulseScale;
            _config.BreakThreshold = _defaults.BreakThreshold;
            _config.DetachedPartMass = _defaults.DetachedPartMass;
            _config.DetachImpulseScale = _defaults.DetachImpulseScale;
            _config.DetachedLifetime = _defaults.DetachedLifetime;
            _config.GlassShatterThreshold = _defaults.GlassShatterThreshold;
            Debug.Log("[DamageTestPanel] DamageConfig restored to Play-start defaults", this);
        }

        public void RefreshCache()
        {
            if (_carRoot == null) return;
            _dentables = _carRoot.GetComponentsInChildren<DentableMesh>(true);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame) _visible = !_visible;
        }

        private void EnsureStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.fontSize = _fontSize + 4;
                _headerStyle.normal.textColor = new Color(1f, 0.85f, 0.4f, 1f);
                _headerStyle.richText = true;
            }
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = _fontSize;
                _labelStyle.richText = true;
            }
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.fontSize = _fontSize;
                _buttonStyle.fixedHeight = _fontSize * 2f;
            }
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.fontSize = _fontSize + 2;
                _boxStyle.fontStyle = FontStyle.Bold;
                _boxStyle.alignment = TextAnchor.UpperLeft;
                _boxStyle.padding = new RectOffset(10, 10, 6, 6);
            }
        }

        private void OnGUI()
        {
            if (!_visible || _config == null || _receiver == null || _carRoot == null) return;
            EnsureStyles();

            var rect = new Rect(10, 10, _panelWidth, _panelHeight);
            GUI.Box(rect, "DAMAGE TEST — F1 to toggle", _boxStyle);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + _fontSize + 12, rect.width - 24, rect.height - _fontSize - 20));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("<b>Dent parameters (live)</b>", _headerStyle);
            _config.DentRadius = LabeledSlider("DentRadius (m)", _config.DentRadius, 0.1f, 2f, "F2");
            _config.DentMaxDepth = LabeledSlider("DentMaxDepth (m)", _config.DentMaxDepth, 0.01f, 1f, "F2");
            _config.DentImpulseScale = LabeledSlider("DentImpulseScale", _config.DentImpulseScale, 0.001f, 0.05f, "F3");
            _config.DentFalloff = LabeledSlider("DentFalloff", _config.DentFalloff, 0.5f, 4f, "F2");
            _config.MinDentImpulse = LabeledSlider("MinDentImpulse", _config.MinDentImpulse, 0f, 20f, "F1");

            GUILayout.Space(10);
            GUILayout.Label("<b>Break parameters</b>", _headerStyle);
            _config.BreakThreshold = LabeledSlider("BreakThreshold", _config.BreakThreshold, 5f, 500f, "F0");
            _config.GlassShatterThreshold = LabeledSlider("GlassShatter", _config.GlassShatterThreshold, 1f, 100f, "F1");

            GUILayout.Space(14);
            GUILayout.Label("<b>Simulated impulse magnitude</b>", _headerStyle);
            _simulatedImpulse = LabeledSlider("Impulse (N·s)", _simulatedImpulse, 5f, 3000f, "F0");

            GUILayout.Space(12);
            GUILayout.Label("<b>Fire dent at region</b>", _headerStyle);
            if (GUILayout.Button("Front nose (Z+)", _buttonStyle)) Fire(_hitFront, Vector3.back);
            if (GUILayout.Button("Rear (Z-)", _buttonStyle)) Fire(_hitRear, Vector3.forward);
            if (GUILayout.Button("Hood (down)", _buttonStyle)) Fire(_hitHood, Vector3.down);
            if (GUILayout.Button("Roof (down)", _buttonStyle)) Fire(_hitRoof, Vector3.down);
            if (GUILayout.Button("Left door", _buttonStyle)) Fire(_hitLeftDoor, Vector3.right);
            if (GUILayout.Button("Right door", _buttonStyle)) Fire(_hitRightDoor, Vector3.left);
            if (GUILayout.Button("Left fender", _buttonStyle)) Fire(_hitLeftFender, Vector3.right);
            if (GUILayout.Button("Right fender", _buttonStyle)) Fire(_hitRightFender, Vector3.left);

            GUILayout.Space(14);
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("RESET ALL DENTS", _buttonStyle)) ResetAll();
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button("RESET PARAMS TO DEFAULTS", _buttonStyle)) RestoreDefaults();
            GUI.backgroundColor = oldColor;

            GUILayout.Space(10);
            int total = 0;
            int contributing = 0;
            if (_dentables != null)
            {
                foreach (var d in _dentables)
                {
                    if (d == null) continue;
                    if (d.LastMutatedVerts > 0) contributing++;
                    total += d.LastMutatedVerts;
                }
            }
            var panelCount = _dentables?.Length ?? 0;
            GUILayout.Label($"Panels tracked: {panelCount}", _labelStyle);
            GUILayout.Label($"Last hit: {total} verts moved across {contributing} panels", _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private float LabeledSlider(string label, float value, float min, float max, string format)
        {
            GUILayout.Label($"{label}: <color=#ffffaa>{value.ToString(format)}</color>", _labelStyle);
            return GUILayout.HorizontalSlider(value, min, max, GUILayout.Height(_fontSize + 4));
        }

        private void Fire(Vector3 localHit, Vector3 localInwardDir)
        {
            var world = _carRoot.TransformPoint(localHit);
            var dir = _carRoot.TransformDirection(localInwardDir);
            var routed = _receiver.DebugImpact(world, dir, _simulatedImpulse);
            Debug.Log($"[DamageTestPanel] Fire at local {localHit} → world {world}, dir {dir}, routed {routed}", this);
        }

        private void ResetAll()
        {
            if (_dentables == null) return;
            int reset = 0;
            foreach (var d in _dentables)
            {
                if (d == null) continue;
                d.ResetToOriginal();
                reset++;
            }
            Debug.Log($"[DamageTestPanel] Reset {reset} panels to original mesh", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_carRoot == null) return;
            Gizmos.matrix = _carRoot.localToWorldMatrix;
            var r = _config != null ? _config.DentRadius : 0.35f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_hitFront, r);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_hitRear, r);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_hitHood, r);
            Gizmos.DrawWireSphere(_hitRoof, r);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_hitLeftDoor, r);
            Gizmos.DrawWireSphere(_hitRightDoor, r);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_hitLeftFender, r);
            Gizmos.DrawWireSphere(_hitRightFender, r);
        }
#endif
    }
}
