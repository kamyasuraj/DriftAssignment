using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.UI.Settings
{
    /// Owns the Settings screen: show/hide, tab switching, pause-on-open,
    /// Esc + backdrop-click to close. UI content is authored in-scene; this
    /// script only manages state and enables the correct tab GameObject.
    [DisallowMultipleComponent]
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Root visuals")]
        [SerializeField] private GameObject _root;      // Backdrop + Frame combined
        [SerializeField] private GameObject _backdrop;  // Clickable dim layer

        [Header("Tabs")]
        [Tooltip("Tab content GameObjects in the same order as _tabButtons")]
        [SerializeField] private GameObject[] _tabContents;
        [Tooltip("Buttons (in the same order as _tabContents). Their onClick is wired at Awake.")]
        [SerializeField] private UnityEngine.UI.Button[] _tabButtons;

        [Header("Behavior")]
        [SerializeField] private bool _pauseWhileOpen = true;
        [SerializeField] private bool _startClosed = true;
        [SerializeField] private Key _toggleKey = Key.Escape;

        public bool IsOpen { get; private set; }

        private int _activeTab;

        private void Awake()
        {
            // Wire tab buttons
            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] == null) continue;
                    int captured = i;
                    _tabButtons[i].onClick.AddListener(() => ShowTab(captured));
                }
            }

            // Wire backdrop click → close
            if (_backdrop != null)
            {
                var trigger = _backdrop.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = _backdrop.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                entry.callback.AddListener(_ => Close());
                trigger.triggers.Add(entry);
            }

            if (_startClosed) Close();
            ShowTab(0);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
            {
                if (IsOpen) Close(); else Open();
            }
        }

        public void Open()
        {
            IsOpen = true;
            if (_root != null) _root.SetActive(true);
            if (_pauseWhileOpen) Time.timeScale = 0f;
        }

        public void Close()
        {
            IsOpen = false;
            if (_root != null) _root.SetActive(false);
            if (_pauseWhileOpen) Time.timeScale = 1f;
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void ShowTab(int index)
        {
            if (_tabContents == null || _tabContents.Length == 0) return;
            _activeTab = Mathf.Clamp(index, 0, _tabContents.Length - 1);
            for (int i = 0; i < _tabContents.Length; i++)
            {
                if (_tabContents[i] != null) _tabContents[i].SetActive(i == _activeTab);
            }
        }
    }
}
