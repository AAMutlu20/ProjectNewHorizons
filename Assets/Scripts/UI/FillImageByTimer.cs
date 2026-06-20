using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utility.Timer;

namespace UI
{
    /// <summary>
    /// Drives one or more Image fillAmounts from a GameTimer's progress.
    /// Used for cooldown rings, cast bars, AOE telegraph fills — anything
    /// that needs a visual "filling up" tied to a timer.
    /// </summary>
    public class FillImageByTimer : MonoBehaviour
    {
        private const float PercentageScale = 100f;

        [SerializeField] private List<Image> imagesToFill = new();
        [SerializeField] private bool toggleImagesWithTimerActive;

        private GameTimer _sourceTimer;

        public GameTimer SourceTimer
        {
            get => _sourceTimer;
            set
            {
                UnsubscribeFromTimer();
                _sourceTimer = value;
                SubscribeToTimer();
            }
        }

        private void OnEnable() => SubscribeToTimer();

        private void OnDisable() => UnsubscribeFromTimer();

        private void SubscribeToTimer()
        {
            if (_sourceTimer == null) return;

            _sourceTimer.Ticked += OnTimerTicked;
            if (toggleImagesWithTimerActive)
                _sourceTimer.RunningStateChanged += OnTimerRunningStateChanged;
        }

        private void UnsubscribeFromTimer()
        {
            if (_sourceTimer == null) return;

            _sourceTimer.Ticked -= OnTimerTicked;
            _sourceTimer.RunningStateChanged -= OnTimerRunningStateChanged;
        }

        private void OnTimerTicked(float deltaTime, float elapsedTime, float percentageComplete)
        {
            SetFillAmount(percentageComplete / PercentageScale);
        }

        private void OnTimerRunningStateChanged(bool wasRunning, bool isRunning)
        {
            if (wasRunning == isRunning) return;
            SetImagesActive(isRunning);
        }

        private void SetFillAmount(float fillAmount)
        {
            foreach (var image in imagesToFill)
                image.fillAmount = fillAmount;
        }

        private void SetImagesActive(bool isActive)
        {
            foreach (var image in imagesToFill)
                image.gameObject.SetActive(isActive);
        }
    }
}
