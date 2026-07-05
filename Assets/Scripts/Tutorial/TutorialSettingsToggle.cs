using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Wires a Settings-menu Toggle ("Enable Tutorial?") to whether the tutorial is
    /// allowed to run. Reads/writes the same PlayerPrefs key TutorialSequenceController
    /// checks in BeginIfNotSeen() -- so switching this off blocks the tutorial entirely,
    /// even if it hasn't been seen yet, and switching it back on re-arms it (respecting
    /// whatever "already completed" state exists separately).
    ///
    /// Attach to: the Toggle GameObject in your Settings menu.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class TutorialSettingsToggle : MonoBehaviour
    {
        private const string EnabledPrefsKey = "TutorialEnabled"; // must match TutorialSequenceController
        private const string CompletedPrefsKey = "TutorialCompleted"; // must match TutorialSequenceController
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.isOn = PlayerPrefs.GetInt(EnabledPrefsKey, 1) == 1; // enabled by default
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void OnToggleChanged(bool isEnabled)
        {
            PlayerPrefs.SetInt(EnabledPrefsKey, isEnabled ? 1 : 0);

            // Turning it back on should actually bring the tutorial back, not just clear
            // one of two independent locks. Without this, re-enabling after it's already
            // been completed once does nothing, since TutorialCompleted still blocks it.
            if (isEnabled)
            {
                PlayerPrefs.DeleteKey(CompletedPrefsKey);
            }

            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
