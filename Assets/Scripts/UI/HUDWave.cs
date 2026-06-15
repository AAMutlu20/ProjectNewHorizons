using Core;
using TMPro;
using UnityEngine;
using Waves;

namespace UI
{
    /// <summary>
    /// Drives the wave counter and rest-period countdown in the HUD.
    /// Subscribes to EventBus — no direct reference to WaveManager.
    ///
    /// Attach to: HUD_Canvas root. Wire TMP text refs in Inspector.
    /// </summary>
    public class HUDWave : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI waveLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private WaveManager waveManager; // needed only for live timer display

        void Start()
        {
            EventBus.Subscribe<WaveStartedEvent> (OnWaveStarted);
            EventBus.Subscribe<WaveCompleteEvent>(OnWaveComplete);

            if (waveLabel) waveLabel.text = "Wave 1";
        }

        void OnDestroy()
        {
            EventBus.Unsubscribe<WaveStartedEvent> (OnWaveStarted);
            EventBus.Unsubscribe<WaveCompleteEvent>(OnWaveComplete);
        }

        void Update()
        {
            // Show live wave timer if active
            if (!timerLabel || !waveManager) return;
            if (waveManager.IsWaveActive)
                timerLabel.text = $"{waveManager.WaveTimer:F0}s";
            else if (waveManager.IsResting)
                timerLabel.text = "Rest...";
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            if (waveLabel)
                waveLabel.text = $"Wave {evt.Wave + 1}";
        }

        private void OnWaveComplete(WaveCompleteEvent evt)
        {
            if (waveLabel)
                waveLabel.text = $"Wave {evt.Wave + 1} complete!";
        }
    }
}
