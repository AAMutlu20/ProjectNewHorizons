using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    /// <summary>
    /// ESC opens/closes the pause menu. Uses GameFreezeController rather
    /// than touching Time.timeScale directly, so pausing while a level-up
    /// choice screen happens to be open doesn't stomp that freeze (and
    /// closing the level-up screen while paused wouldn't incorrectly
    /// resume gameplay either — both freezes must release before time runs again).
    ///
    /// No on-screen pause button per the design reference — ESC only, as is
    /// standard for desktop. Mobile gets a dedicated pause button elsewhere
    /// (see HUDMobileTopBar) that calls TogglePause directly.
    ///
    /// Attach to: a persistent [Systems] GameObject (not the HUD canvas
    /// itself, so it keeps working even if the HUD canvas is hidden).
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private const string FreezeReason = "PauseMenu";

        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private InputActionReference pauseAction;

        private bool _isPaused;

        private void Awake()
        {
            SetMenuVisible(false);
        }

        private void OnEnable()
        {
            if (pauseAction) pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            if (pauseAction) pauseAction.action.Disable();

            // Defensive: if this controller is disabled while paused (e.g. scene
            // unload), release the freeze rather than leaving Time.timeScale at 0.
            if (_isPaused) GameFreezeController.ReleaseFreeze(FreezeReason);
        }

        private void Update()
        {
            if (pauseAction && pauseAction.action.WasPerformedThisFrame())
                TogglePause();
        }

        /// <summary>Public so a mobile pause button (no ESC key) can call this directly.</summary>
        public void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;
            GameFreezeController.RequestFreeze(FreezeReason);
            SetMenuVisible(true);
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;
            GameFreezeController.ReleaseFreeze(FreezeReason);
            SetMenuVisible(false);
        }

        private void SetMenuVisible(bool isVisible)
        {
            if (pauseMenuRoot) pauseMenuRoot.SetActive(isVisible);
        }
    }
}
