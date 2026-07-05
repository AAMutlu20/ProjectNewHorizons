using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BloodlinesUI;

namespace Core
{
    /// <summary>
    /// Bridges the "Canvas (Loading)" fake-progress bar (LoadingSceneManager, from the
    /// BloodlinesUI asset) with a real async scene load.
    ///
    /// LoadingSceneManager's bar always finishes after loadingDuration + hideDelay
    /// seconds, regardless of actual load progress -- it's a fixed-time visual animation,
    /// not wired to AsyncOperation.progress. This script starts the real scene load in
    /// parallel and only activates it once BOTH the fake bar has finished its animation
    /// AND the real scene is actually loaded (capped at 90% until then) -- whichever
    /// takes longer. That keeps the bar visually honest (it can never "finish" before
    /// the scene is actually ready) while still letting the animation play out in full
    /// even on a fast load.
    ///
    /// Attach to: a GameObject in your MainMenu scene. Point your Play button's
    /// OnClick at this component's StartGame() instead of loading the scene directly.
    /// </summary>
    public class MainMenuLoadingController : MonoBehaviour
    {
        [Header("Loading screen")]
        [Tooltip("The root 'Canvas (Loading)' GameObject. Should start inactive in the scene.")]
        [SerializeField] private GameObject loadingCanvas;
        [SerializeField] private LoadingSceneManager loadingSceneManager;

        [Header("Timing")]
        [Tooltip("Must match loadingDuration + hideDelay on LoadingSceneManager's inspector " +
                 "values, so the fake bar and the real scene swap stay in sync. If you change " +
                 "one, change this too.")]
        [SerializeField] private float minimumLoadingTime = 4f;

        [Header("Optional: block input during load")]
        [Tooltip("Any buttons/canvas groups you want disabled while loading (e.g. your main menu " +
                 "canvas), so the player can't double-click Play or navigate away mid-load.")]
        [SerializeField] private CanvasGroup[] disableWhileLoading;

        [Header("Shader/PSO warmup")]
        [SerializeField] private PsoWarmupController psoWarmup; // optional -- leave unassigned to skip

        private void Awake()
        {
            Debug.Assert(loadingCanvas, "MainMenuLoadingController: loadingCanvas ref missing");
            Debug.Assert(loadingSceneManager, "MainMenuLoadingController: loadingSceneManager ref missing");
            loadingCanvas.SetActive(false);
        }

        /// <summary>Call this from your Play button instead of loading the scene directly.</summary>
        public void StartGame()
        {
            SetInteractable(false);
            loadingCanvas.SetActive(true);
            loadingSceneManager.StartLoading();
            if (psoWarmup) psoWarmup.BeginWarmup();
            StartCoroutine(LoadNextSceneRoutine());
        }

        private IEnumerator LoadNextSceneRoutine()
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneIndex);
            op.allowSceneActivation = false;

            float elapsed = 0f;
            // Wait until the fake bar's animation time has passed AND the real scene is
            // ready (Unity holds progress at 0.9 until allowSceneActivation is set true).
            // Uses unscaled time deliberately: if Time.timeScale is ever 0 at this point
            // (paused, frozen, whatever), Time.deltaTime would be 0 forever and this loop
            // would never exit, permanently stalling the scene transition.
            while (elapsed < minimumLoadingTime || op.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            op.allowSceneActivation = true;
        }

        private void SetInteractable(bool interactable)
        {
            if (disableWhileLoading == null) return;
            foreach (var group in disableWhileLoading)
            {
                if (!group) continue;
                group.interactable = interactable;
                group.blocksRaycasts = interactable;
            }
        }
    }
}
