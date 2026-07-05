using UnityEngine;
using UnityEngine.EventSystems;

namespace DriftAssignment.UI.Settings
{
    /// Small click handler for the HUD Settings icon. Opens the SettingsPanel
    /// on pointer down. Doesn't perform hold-based input like TouchButton.
    [DisallowMultipleComponent]
    public class OpenSettingsButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SettingsPanel _panel;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_panel != null) _panel.Toggle();
        }
    }
}
