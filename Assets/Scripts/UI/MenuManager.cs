using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MenuManager : MonoBehaviour
    {
        private const string FreezeReason = "Settings";

        [SerializeField] private GameObject gameOverMenu;

        public GameObject overlay;
        public GameObject settings;

        private void Awake()
        {
            Debug.Assert(gameOverMenu, "MenuManager: gameOverMenu ref missing -- OnPlayerDied will throw and silently fail to show it");
        }

        void Start()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnDestroy()
        {
            // Without this, a MenuManager destroyed on scene unload (e.g. the
            // MainMenu instance, once the game scene loads) leaves a stale
            // delegate registered in EventBus forever -- it'll still get
            // invoked on the next PlayerDiedEvent, throwing when it touches
            // gameOverMenu on an already-destroyed object.
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        // Closes the game
        public void QuitGame()
        {
            Application.Quit();
        }

        // Reloads current scene 
        public void Restart()
        {
            GameFreezeController.ClearAll(); // scene is unloading — don't let a stale reason leak into the reload
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Loads scene 0
        public void MainMenu()
        {
            GameFreezeController.ClearAll(); // scene is unloading — don't let a stale reason leak into the main menu
            SceneManager.LoadScene(0);
        }

        // Loads previous scene 
        public void GoAgain()
        {
            GameFreezeController.ClearAll(); // scene is unloading — don't let a stale reason leak into the previous scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }

        //Opens the Setting menu
        public void Setting()
        {
            overlay.gameObject.SetActive(false);
            settings.gameObject.SetActive(true);
            GameFreezeController.RequestFreeze(FreezeReason);
        }

        //Closes the Settings menu
        public void Back()
        {
            overlay.gameObject.SetActive(true);
            settings.gameObject.SetActive(false);
            GameFreezeController.ReleaseFreeze(FreezeReason);
        }

        private void OnPlayerDied(PlayerDiedEvent @event)
        {
            GameFreezeController.RequestFreeze(FreezeReason);
            gameOverMenu.SetActive(true);
        }

    }
}
