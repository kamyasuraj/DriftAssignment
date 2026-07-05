using DriftAssignment.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DriftAssignment.UI.Hud
{
    /// A single UI button that maps a pointer press to a specific HUD action.
    /// Attach to a UGUI Image/Button. Choose which control on the shared
    /// TouchInputProvider it drives via _action, and whether it's press-hold
    /// (throttle, brake, steer) or one-shot (shift up/down, camera cycle).
    ///
    /// Pointer-up also fires when the pointer leaves the button while held,
    /// so dragging off does not "stick" the input.
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class TouchButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Action
        {
            SteerLeft, SteerRight,
            Throttle, Brake, HandBrake,
            ShiftUp, ShiftDown,
            CameraCycle, LookBack,
        }

        [SerializeField] private TouchInputProvider _input;
        [SerializeField] private Action _action = Action.Throttle;
        [SerializeField] private Camera.CameraRig _cameraRig; // only used by Camera actions
        [Tooltip("Optional press-tint target. If null, no visual feedback is applied. Alpha of the target is preserved from its authored value — only RGB is tinted.")]
        [SerializeField] private Graphic _visualTarget;
        [SerializeField] private Color _idleColor  = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _pressColor = new Color(1f, 0.85f, 0.4f, 1f);
        [SerializeField] private bool _debugLog = false;

        private bool _held;
        private float _originalAlpha = 1f;

        private void Awake()
        {
            // Only apply visual feedback if the user explicitly assigned a target.
            // Otherwise we'd stomp the button's authored alpha.
            if (_visualTarget != null)
            {
                _originalAlpha = _visualTarget.color.a;
                ApplyColor(false);
            }
        }

        public void OnPointerDown(PointerEventData ev)
        {
            _held = true;
            ApplyColor(true);
            Apply(true);
        }

        public void OnPointerUp(PointerEventData ev)
        {
            if (!_held) return;
            _held = false;
            ApplyColor(false);
            Apply(false);
        }

        public void OnPointerExit(PointerEventData ev)
        {
            // If the finger slid off while still pressed, release the input
            // so the car doesn't get stuck accelerating.
            if (!_held) return;
            _held = false;
            ApplyColor(false);
            Apply(false);
        }

        private void Apply(bool on)
        {
            if (_debugLog) Debug.Log($"[TouchButton] {name} {_action} → {(on ? "DOWN" : "up")}", this);
            if (_input == null) return;
            switch (_action)
            {
                case Action.SteerLeft:  _input.SetSteerLeft(on);  break;
                case Action.SteerRight: _input.SetSteerRight(on); break;
                case Action.Throttle:   _input.SetThrottle(on);   break;
                case Action.Brake:      _input.SetBrake(on);      break;
                case Action.HandBrake:  _input.SetHandBrake(on);  break;
                case Action.ShiftUp:    if (on) _input.QueueShiftUp();   break;
                case Action.ShiftDown:  if (on) _input.QueueShiftDown(); break;
                case Action.CameraCycle:
                    if (on && _cameraRig != null) _cameraRig.CycleCamera();
                    break;
                case Action.LookBack:
                    if (_cameraRig != null) _cameraRig.SetLookBack(on);
                    break;
            }
        }

        private void ApplyColor(bool pressed)
        {
            if (_visualTarget == null) return;
            // Only tint RGB — preserve whatever alpha the artist authored.
            var rgb = pressed ? _pressColor : _idleColor;
            _visualTarget.color = new Color(rgb.r, rgb.g, rgb.b, _originalAlpha);
        }
    }
}
