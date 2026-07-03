using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    /// <summary>
    /// Drives the four volume sliders in the settings menu.
    /// Wire each Slider in the Inspector, then wire each slider's
    /// OnValueChanged event to the matching method here.
    ///
    /// Reads saved values from AudioManager on Enable so the sliders
    /// always reflect the current (persisted) volume when the settings
    /// panel opens.
    ///
    /// Attach to: your settings panel GameObject.
    /// </summary>
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;

        private void OnEnable()
        {
            // Populate sliders from saved values without triggering OnValueChanged
            if (!AudioManager.Instance) return;
            SetSliderSilently(masterSlider, AudioManager.Instance.GetMasterVolume());
            SetSliderSilently(musicSlider,  AudioManager.Instance.GetMusicVolume());
            SetSliderSilently(sfxSlider,    AudioManager.Instance.GetSfxVolume());
            SetSliderSilently(uiSlider,     AudioManager.Instance.GetUiVolume());
        }

        // Wire each of these to the matching slider's OnValueChanged event in the Inspector.
        public void OnMasterChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
        public void OnMusicChanged (float value) => AudioManager.Instance?.SetMusicVolume(value);
        public void OnSfxChanged   (float value) => AudioManager.Instance?.SetSfxVolume(value);
        public void OnUiChanged    (float value) => AudioManager.Instance?.SetUiVolume(value);

        // Sets slider value without firing OnValueChanged (avoids feedback loop on open)
        private static void SetSliderSilently(Slider slider, float value)
        {
            if (!slider) return;
            slider.SetValueWithoutNotify(value);
        }
    }
}
