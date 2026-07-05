using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DriftAssignment.UI.Settings
{
    /// Attach next to a TMP_Text to make its <link="url">…</link> tags clickable.
    /// On pointer click, detects which link (if any) was hit and calls
    /// Application.OpenURL with the link's ID.
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class TmpLinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_text == null) return;
            var cam = eventData.pressEventCamera;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, cam);
            if (linkIndex < 0) return;
            var link = _text.textInfo.linkInfo[linkIndex];
            var url = link.GetLinkID();
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }
    }
}
