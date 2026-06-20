using UnityEngine;
using UnityEngine.UI;
using Utility.Timer;

namespace VFX
{
    /// <summary>
    /// Flashes a UI Image to maxAlpha and back to its original color over
    /// flashTimer's duration. Call FlashImage() to trigger it — for example,
    /// from PlayerHealth when the player takes damage.
    /// </summary>
    public class ImageFlash : MonoBehaviour
    {
        private const float PercentageScale = 100f;

        [SerializeField] private Image imageToFlash;
        [SerializeField] private GameTimer flashTimer = new();
        [SerializeField] private float maxAlpha = 1f;

        private Color _originalColor;

        private void Start()
        {
            _originalColor = imageToFlash.color;

            flashTimer.Completed += BeginFadeBackToOriginal;
            flashTimer.Ticked += UpdateFlashAlpha;
            flashTimer.CountdownFinished += RestoreOriginalColor;
        }

        private void Update()
        {
            flashTimer.UpdateTimer(Time.deltaTime);
        }

        public void FlashImage()
        {
            flashTimer.Start();
        }

        private void BeginFadeBackToOriginal()
        {
            flashTimer.Start(countingDown: true);
        }

        private void RestoreOriginalColor()
        {
            imageToFlash.color = _originalColor;
        }

        private void UpdateFlashAlpha(float deltaTime, float elapsedTime, float percentageComplete)
        {
            var color = imageToFlash.color;
            color.a = (maxAlpha / PercentageScale) * percentageComplete;
            imageToFlash.color = color;
        }
    }
}
