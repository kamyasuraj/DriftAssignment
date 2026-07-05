using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.Camera
{
    /// Cycles a set of Cinemachine cameras (Chase / Hood / Cinematic) via the
    /// active-index priority pattern, and temporarily activates a rear-facing
    /// LookBack camera while a key is held. Cinemachine handles the blend curves.
    ///
    /// Editor: press C to cycle, hold V for look-back.
    /// On-device (Phase 4): the touch HUD will drive CycleCamera() / SetLookBack().
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [Header("Cycle order")]
        [SerializeField] private CinemachineCamera[] _cycleCameras;

        [Header("Look-back (hold to activate)")]
        [SerializeField] private CinemachineCamera _lookBackCamera;

        [Header("Priority levels")]
        [Tooltip("Priority given to the currently active cycle camera")]
        [SerializeField] private int _activePriority = 30;
        [Tooltip("Priority given to inactive cycle cameras")]
        [SerializeField] private int _idlePriority = 10;
        [Tooltip("Priority given to LookBack while its hold-key is down (must exceed active)")]
        [SerializeField] private int _lookBackActivePriority = 40;

        [Header("Input")]
        [SerializeField] private Key _cycleKey = Key.C;
        [SerializeField] private Key _lookBackKey = Key.V;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private int _activeIndex;
        private bool _lookBackActive;

        public int ActiveIndex => _activeIndex;
        public CinemachineCamera ActiveCamera => (_cycleCameras != null && _activeIndex < _cycleCameras.Length) ? _cycleCameras[_activeIndex] : null;

        private void OnEnable()
        {
            ApplyPriorities();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[_cycleKey].wasPressedThisFrame) CycleCamera();

            var lb = kb[_lookBackKey].isPressed;
            if (lb != _lookBackActive) SetLookBack(lb);
        }

        public void CycleCamera()
        {
            if (_cycleCameras == null || _cycleCameras.Length == 0) return;
            _activeIndex = (_activeIndex + 1) % _cycleCameras.Length;
            ApplyPriorities();
            if (_debugLog) Debug.Log($"[CameraRig] Cycled → {ActiveCamera?.name} (idx {_activeIndex})", this);
        }

        public void SetLookBack(bool on)
        {
            _lookBackActive = on;
            if (_lookBackCamera == null) return;
            _lookBackCamera.Priority = new PrioritySettings
            {
                Value = on ? _lookBackActivePriority : _idlePriority
            };
            if (_debugLog) Debug.Log($"[CameraRig] LookBack {(on ? "ON" : "off")}", this);
        }

        public void SelectCamera(int index)
        {
            if (_cycleCameras == null || _cycleCameras.Length == 0) return;
            _activeIndex = Mathf.Clamp(index, 0, _cycleCameras.Length - 1);
            ApplyPriorities();
        }

        private void ApplyPriorities()
        {
            if (_cycleCameras == null) return;
            for (int i = 0; i < _cycleCameras.Length; i++)
            {
                if (_cycleCameras[i] == null) continue;
                _cycleCameras[i].Priority = new PrioritySettings
                {
                    Value = i == _activeIndex ? _activePriority : _idlePriority
                };
            }
            if (_lookBackCamera != null && !_lookBackActive)
            {
                _lookBackCamera.Priority = new PrioritySettings { Value = _idlePriority };
            }
        }
    }
}
