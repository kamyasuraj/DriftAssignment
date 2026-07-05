using DriftAssignment.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace DriftAssignment.UI.Hud
{
    /// Enables the ShiftUp / ShiftDown HUD buttons only when the transmission
    /// is in MANUAL mode. Watches TuningState.Changed so switching the mode
    /// in the Settings panel takes effect immediately.
    ///
    /// Toggles both TouchButton.enabled and Image.raycastTarget (so the
    /// button truly stops receiving pointer events, not just visually).
    /// Also dims the button via CanvasGroup.alpha when disabled.
    [DisallowMultipleComponent]
    public class GearButtonsController : MonoBehaviour
    {
        [SerializeField] private TuningState _tuning;
        [Tooltip("Buttons that should only be interactive in MANUAL mode (ShiftUp, ShiftDown)")]
        [SerializeField] private GameObject[] _manualOnlyButtons;

        [Header("Look")]
        [Range(0f, 1f)] [SerializeField] private float _disabledAlpha = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _enabledAlpha = 1.0f;

        private void OnEnable()
        {
            if (_tuning != null)
            {
                _tuning.Changed += OnTuningChanged;
                OnTuningChanged();
            }
        }

        private void OnDisable()
        {
            if (_tuning != null) _tuning.Changed -= OnTuningChanged;
        }

        private void OnTuningChanged()
        {
            if (_tuning == null || _manualOnlyButtons == null) return;
            var manual = !_tuning.AutomaticTransmission;
            foreach (var go in _manualOnlyButtons)
            {
                if (go == null) continue;
                var tb = go.GetComponent<TouchButton>();
                if (tb != null) tb.enabled = manual;
                var img = go.GetComponent<Image>();
                if (img != null) img.raycastTarget = manual;
                // Also let child Image(s) (like the button's Icon child) block raycasts
                foreach (var childImg in go.GetComponentsInChildren<Image>(true))
                {
                    if (childImg == img) continue;
                    childImg.raycastTarget = manual;
                }
                // Alpha dim via CanvasGroup so the whole button + icon fade together
                var cg = go.GetComponent<CanvasGroup>();
                if (cg == null) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = manual ? _enabledAlpha : _disabledAlpha;
                cg.interactable = manual;
                cg.blocksRaycasts = manual;
            }
        }
    }
}
