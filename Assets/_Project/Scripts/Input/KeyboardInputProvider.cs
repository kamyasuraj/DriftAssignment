using DriftAssignment.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.Input
{
    public class KeyboardInputProvider : MonoBehaviour, IInputProvider
    {
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool HandBrake { get; private set; }
        public bool ShiftUp { get; private set; }
        public bool ShiftDown { get; private set; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                Throttle = 0f;
                Brake = 0f;
                Steer = 0f;
                HandBrake = false;
                ShiftUp = false;
                ShiftDown = false;
                return;
            }

            Throttle = kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f;
            Brake = kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f;

            var left = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            var right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            Steer = (right ? 1f : 0f) - (left ? 1f : 0f);

            HandBrake = kb.spaceKey.isPressed;
            ShiftUp = kb.eKey.wasPressedThisFrame;
            ShiftDown = kb.qKey.wasPressedThisFrame;
        }
    }
}
