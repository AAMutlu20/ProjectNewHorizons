using System;
using UnityEngine;

namespace Utility.Timer
{
    /// <summary>
    /// A serializable countdown/countup timer with tick and completion events.
    /// Call UpdateTimer(Time.deltaTime) every frame to advance it — the timer
    /// itself has no Update loop, so the caller controls its cadence.
    /// </summary>
    [Serializable]
    public class GameTimer
    {
        private const float PercentageScale = 100f;
        private const float SecondsPerHour = 3600f;
        private const float SecondsPerMinute = 60f;

        [SerializeField] private float duration;
        [SerializeField] private float minRandomDuration;
        [SerializeField] private float maxRandomDuration;

        private float _elapsed;
        private bool _isRunning;
        private bool _isCountingDown;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        public float PercentageComplete => duration > 0f
            ? (_elapsed / duration) * PercentageScale
            : 0f;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                var wasRunning = _isRunning;
                _isRunning = value;
                if (wasRunning != value)
                    RunningStateChanged?.Invoke(wasRunning, value);
            }
        }

        /// <summary>Fired every call to UpdateTimer. Args: deltaTime, elapsedTime, percentageComplete.</summary>
        public event Action<float, float, float> Ticked;

        /// <summary>Fired once when a count-up timer reaches its duration.</summary>
        public event Action Completed;

        /// <summary>Fired once when a count-down timer reaches zero.</summary>
        public event Action CountdownFinished;

        /// <summary>Fired when IsRunning changes. Args: wasRunning, isRunning.</summary>
        public event Action<bool, bool> RunningStateChanged;

        public GameTimer() { }

        public GameTimer(float timerDuration)
        {
            duration = timerDuration;
        }

        /// <summary>
        /// Starts the timer counting up from zero (or down from Duration if
        /// countingDown is true).
        /// </summary>
        public void Start(bool countingDown = false)
        {
            _isCountingDown = countingDown;
            _elapsed = countingDown ? duration : 0f;
            IsRunning = true;
        }

        /// <summary>Starts the timer with a duration picked from a random target time range.</summary>
        public void StartWithRandomDuration()
        {
            duration = UnityEngine.Random.Range(minRandomDuration, maxRandomDuration);
            Start();
        }

        public void Pause() => IsRunning = false;

        public void Resume() => IsRunning = true;

        /// <summary>Advances the timer by deltaTime. Call this once per frame from the owning MonoBehaviour.</summary>
        public void UpdateTimer(float deltaTime)
        {
            if (!IsRunning) return;

            _elapsed += _isCountingDown ? -deltaTime : deltaTime;
            Ticked?.Invoke(deltaTime, _elapsed, PercentageComplete);

            switch (_isCountingDown)
            {
                case true when _elapsed <= 0f:
                    _elapsed = 0f;
                    IsRunning = false;
                    CountdownFinished?.Invoke();
                    break;
                case false when _elapsed >= duration:
                    _elapsed = duration;
                    IsRunning = false;
                    Completed?.Invoke();
                    break;
            }
        }

        public void ResetElapsed(bool toFullDuration = false)
        {
            _elapsed = toFullDuration ? duration : 0f;
        }

        /// <summary>Immediately jumps to the timer's end state, as if it had finished naturally.</summary>
        public void SkipToEnd()
        {
            _elapsed = _isCountingDown ? 0f : duration;
        }

        public void ClearAllListeners()
        {
            Ticked = null;
            Completed = null;
            CountdownFinished = null;
            RunningStateChanged = null;
        }

        public string GetRemainingTimeFormatted()
        {
            var remainingSeconds = _isCountingDown ? _elapsed : duration - _elapsed;

            var hours = (int)(remainingSeconds / SecondsPerHour);
            remainingSeconds -= hours * SecondsPerHour;

            var minutes = (int)(remainingSeconds / SecondsPerMinute);
            remainingSeconds -= minutes * SecondsPerMinute;

            var formatted = string.Empty;
            if (hours > 0) formatted += $"{hours}:";
            if (minutes > 0) formatted += $"{minutes}:";
            formatted += $"{remainingSeconds:F0}";

            return formatted;
        }
    }
}
