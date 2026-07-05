using DriftAssignment.Vehicle;
using UnityEngine;

namespace DriftAssignment.UI
{
    /// OnGUI diagnostic bar stretched across the bottom of the screen. Shows
    /// stats that the game HUD does NOT already display: FPS, Throttle, Brake,
    /// Handbrake. Zero UI-canvas dependencies.
    [DisallowMultipleComponent]
    public class DiagnosticHud : MonoBehaviour
    {
        [SerializeField] private CarController _car;
        [SerializeField] private int _barHeight = 46;
        [SerializeField] private int _fontSize = 18;
        [SerializeField] private int _statSpacing = 32;
        [SerializeField] private Color _bgColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _labelColor = new Color(0.75f, 0.9f, 0.75f, 1f);

        private float _fpsSmoothed;
        private GUIStyle _statStyle;
        private GUIStyle _boxStyle;
        private Texture2D _bgTex;

        private void Awake()
        {
            if (_car == null) _car = GetComponent<CarController>();
        }

        private void Update()
        {
            var instant = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fpsSmoothed = Mathf.Lerp(_fpsSmoothed, instant, Time.unscaledDeltaTime * 4f);
        }

        private void EnsureStyles()
        {
            if (_bgTex == null)
            {
                _bgTex = new Texture2D(1, 1);
                _bgTex.SetPixel(0, 0, _bgColor);
                _bgTex.Apply();
            }
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = _bgTex;
                _boxStyle.border = new RectOffset(0, 0, 0, 0);
                _boxStyle.padding = new RectOffset(0, 0, 0, 0);
            }
            if (_statStyle == null)
            {
                _statStyle = new GUIStyle(GUI.skin.label);
                _statStyle.fontSize = _fontSize;
                _statStyle.fontStyle = FontStyle.Bold;
                _statStyle.alignment = TextAnchor.MiddleCenter;
                _statStyle.richText = true;
                _statStyle.normal.textColor = _labelColor;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            var barRect = new Rect(0, Screen.height - _barHeight, Screen.width, _barHeight);
            GUI.Box(barRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(barRect);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Stat("FPS", $"{_fpsSmoothed:0}");
            if (_car != null)
            {
                Gap();
                Stat("THROTTLE", $"{_car.ThrottleInput:0.00}");
                Gap();
                Stat("BRAKE", $"{_car.BrakeInput:0.00}");
                Gap();
                Stat("HANDBRAKE", _car.HandBrakeActive ? "<color=#ff7070>ON</color>" : "off");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Stat(string label, string value)
        {
            var text = $"<color=#c0e0c0>{label}</color> <color=#ffff99>{value}</color>";
            GUILayout.Label(text, _statStyle, GUILayout.Height(_barHeight));
        }

        private void Gap()
        {
            GUILayout.Space(_statSpacing);
        }

        private void OnDestroy()
        {
            if (_bgTex != null) Destroy(_bgTex);
        }
    }
}
