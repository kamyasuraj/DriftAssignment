using DriftAssignment.Camera;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftAssignment.UI.Hud
{
    /// Swaps a target Image's sprite whenever the CameraRig cycles, and
    /// optionally updates a TMP label with the camera name. Icon + label
    /// order mirrors CameraRig._cycleCameras (Chase 0 → Cinematic 1 → Broadcast 2).
    [DisallowMultipleComponent]
    public class CameraIconSwapper : MonoBehaviour
    {
        [SerializeField] private CameraRig _cameraRig;
        [Tooltip("The Image whose sprite gets replaced on camera change (usually the child 'Icon' image on the Camera button)")]
        [SerializeField] private Image _targetImage;
        [Tooltip("Icons in the same order as CameraRig._cycleCameras")]
        [SerializeField] private Sprite _chaseIcon;
        [SerializeField] private Sprite _cinematicIcon;
        [SerializeField] private Sprite _broadcastIcon;

        [Header("Camera name label (optional)")]
        [Tooltip("A TMP text that will display the camera's name on cycle. Leave null to skip.")]
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private string _chaseLabel = "CHASE";
        [SerializeField] private string _cinematicLabel = "CINEMATIC";
        [SerializeField] private string _broadcastLabel = "BROADCAST";

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private void OnEnable()
        {
            if (_cameraRig != null)
            {
                _cameraRig.CameraChanged += OnCameraChanged;
                OnCameraChanged(_cameraRig.ActiveIndex);
            }
        }

        private void OnDisable()
        {
            if (_cameraRig != null) _cameraRig.CameraChanged -= OnCameraChanged;
        }

        private void OnCameraChanged(int index)
        {
            var iconOk = false;
            if (_targetImage != null)
            {
                var sprite = GetIcon(index);
                if (sprite != null) { _targetImage.sprite = sprite; iconOk = true; }
            }
            var lbl = string.Empty;
            if (_labelText != null)
            {
                lbl = GetLabel(index);
                _labelText.text = lbl;
            }
            if (_debugLog) Debug.Log($"[CameraIconSwapper] idx={index} icon={iconOk} label='{lbl}' target={(_targetImage!=null)} labelRef={(_labelText!=null)}", this);
        }

        private Sprite GetIcon(int index)
        {
            switch (index)
            {
                case 0: return _chaseIcon;
                case 1: return _cinematicIcon;
                case 2: return _broadcastIcon;
                default: return null;
            }
        }

        private string GetLabel(int index)
        {
            switch (index)
            {
                case 0: return _chaseLabel;
                case 1: return _cinematicLabel;
                case 2: return _broadcastLabel;
                default: return string.Empty;
            }
        }
    }
}
