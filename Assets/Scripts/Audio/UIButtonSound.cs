using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    /// <summary>
    /// Add to any Button to make it play a click sound automatically.
    /// No wiring needed beyond dropping this component on the button —
    /// it hooks into Button.onClick in Awake.
    ///
    /// The clip is chosen randomly from AudioManager.buttonClickSounds
    /// each time, so a single component gives you variety across all buttons.
    ///
    /// Attach to: every Button GameObject in your UI.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            AudioManager.Instance?.PlayButtonClick();
        }
    }
}
