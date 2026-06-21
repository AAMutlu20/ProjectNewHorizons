using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Mobile-only: a visible pause button (no ESC key on touch devices),
    /// calling the same PauseMenuController as desktop's ESC binding. The
    /// timer (HUDTimeAlive) and XP bar (HUDExperience) are shared components
    /// reused as-is on the mobile canvas — this script only adds the button
    /// mobile needs that desktop doesn't.
    ///
    /// Attach to: Mobile HUD canvas, top area, alongside the pause button.
    /// </summary>
    public class HUDMobileTopBar : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private PauseMenuController pauseMenuController;

        private void Awake()
        {
            Debug.Assert(pauseButton, "HUDMobileTopBar: pauseButton not assigned.", this);
            Debug.Assert(pauseMenuController, "HUDMobileTopBar: pauseMenuController not assigned.", this);

            pauseButton.onClick.AddListener(pauseMenuController.TogglePause);
        }
    }
}
