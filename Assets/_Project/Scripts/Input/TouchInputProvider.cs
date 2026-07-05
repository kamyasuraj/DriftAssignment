using DriftAssignment.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.Input
{
    /// IInputProvider driven by touch/UI buttons. HUD widgets call the setter
    /// methods each frame; internal smoothing produces analog-feel steering
    /// from digital left/right buttons.
    ///
    /// _keyboardFallback = true lets the editor keyboard drive the same
    /// state too, so we can Play-test without touching the on-screen UI.
    [DisallowMultipleComponent]
    public class TouchInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Steering smoothing (analog from digital buttons)")]
        [Tooltip("Seconds to ramp steer from 0 to full lock when a direction button is held")]
        [SerializeField] private float _steerRampSec = 0.35f;
        [Tooltip("Seconds to return steer to 0 when both buttons released")]
        [SerializeField] private float _steerReturnSec = 0.2f;

        [Header("Editor fallback")]
        [SerializeField] private bool _keyboardFallback = true;

        // Raw digital state written by HUD buttons
        private bool _leftHeld, _rightHeld;
        private bool _throttleHeld, _brakeHeld;
        private bool _handBrakeHeld;
        private bool _shiftUpQueued, _shiftDownQueued;

        // Smoothed values
        private float _smoothedSteer;

        // IInputProvider
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steer => _smoothedSteer;
        public bool HandBrake { get; private set; }
        public bool ShiftUp { get; private set; }
        public bool ShiftDown { get; private set; }

        // ---- HUD widget setters ----
        public void SetSteerLeft(bool on)  { _leftHeld = on; }
        public void SetSteerRight(bool on) { _rightHeld = on; }
        public void SetThrottle(bool on)   { _throttleHeld = on; }
        public void SetBrake(bool on)      { _brakeHeld = on; }
        public void SetHandBrake(bool on)  { _handBrakeHeld = on; }
        public void QueueShiftUp()         { _shiftUpQueued = true; }
        public void QueueShiftDown()       { _shiftDownQueued = true; }

        private void Update()
        {
            var kb = _keyboardFallback ? Keyboard.current : null;

            var left = _leftHeld || (kb != null && kb[Key.A].isPressed);
            var right = _rightHeld || (kb != null && kb[Key.D].isPressed);
            var throttle = _throttleHeld || (kb != null && kb[Key.W].isPressed);
            var brake = _brakeHeld || (kb != null && kb[Key.S].isPressed);
            var handbrake = _handBrakeHeld || (kb != null && kb[Key.Space].isPressed);

            // Steer target: -1, 0, +1 from digital state — smoothed toward target
            float target = 0f;
            if (right) target += 1f;
            if (left)  target -= 1f;
            var ramp = Mathf.Approximately(target, 0f) ? 1f / Mathf.Max(0.01f, _steerReturnSec) : 1f / Mathf.Max(0.01f, _steerRampSec);
            _smoothedSteer = Mathf.MoveTowards(_smoothedSteer, target, ramp * Time.deltaTime);

            Throttle = throttle ? 1f : 0f;
            Brake = brake ? 1f : 0f;
            HandBrake = handbrake;

            // One-shot shift edges consumed exactly once per queued press
            ShiftUp = _shiftUpQueued || (kb != null && kb[Key.E].wasPressedThisFrame);
            ShiftDown = _shiftDownQueued || (kb != null && kb[Key.Q].wasPressedThisFrame);
            _shiftUpQueued = false;
            _shiftDownQueued = false;
        }
    }
}
