using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    /// <summary>
    /// Drives the four volume sliders in the settings menu.
    /// Hooks into each slider's onValueChanged in Awake so no manual
    /// UnityEvent wiring is needed in the Inspector — just assign the
    /// four Slider references and it works.
    /// </summary>
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;

        private void Awake()
        {
            // Set slider ranges
            SetupSlider(masterSlider);
            SetupSlider(musicSlider);
            SetupSlider(sfxSlider);
            SetupSlider(uiSlider);

            // Hook listeners — no Inspector event wiring needed
            masterSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMasterVolume(v));
            musicSlider?.onValueChanged.AddListener(v  => AudioManager.Instance?.SetMusicVolume(v));
            sfxSlider?.onValueChanged.AddListener(v    => AudioManager.Instance?.SetSfxVolume(v));
            uiSlider?.onValueChanged.AddListener(v     => AudioManager.Instance?.SetUiVolume(v));
        }

        private void OnEnable()
        {
            // Refresh sliders to reflect current saved values whenever panel opens
            if (!AudioManager.Instance) return;
            masterSlider?.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
            musicSlider?.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            sfxSlider?.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
            uiSlider?.SetValueWithoutNotify(AudioManager.Instance.GetUiVolume());
        }

        private static void SetupSlider(Slider s)
        {
            if (!s) return;
            s.minValue     = 0f;
            s.maxValue     = 1f;
            s.wholeNumbers = false;
        }
    }
}
