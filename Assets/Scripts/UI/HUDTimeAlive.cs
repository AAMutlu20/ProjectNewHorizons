using TMPro;
using UnityEngine;
using Waves;

namespace UI
{
    /// <summary>
    /// Top-right "Time alive" readout. Reuses WaveDirector.ElapsedGameTime
    /// rather than running its own clock — that value already counts up
    /// from run start and never resets, which is exactly what "time alive"
    /// means here (the run starts when WaveDirector starts).
    ///
    /// Attach to: Desktop HUD canvas, top-right.
    /// </summary>
    public class HUDTimeAlive : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeAliveLabel;
        [SerializeField] private WaveDirector waveDirector;

        private void Awake()
        {
            Debug.Assert(waveDirector, "HUDTimeAlive: waveDirector not assigned.", this);
        }

        private void Update()
        {
            if (!timeAliveLabel) return;

            var totalSeconds = waveDirector.ElapsedGameTime;
            var minutes = Mathf.FloorToInt(totalSeconds / 60f);
            var seconds = Mathf.FloorToInt(totalSeconds % 60f);

            timeAliveLabel.text = $"{minutes}:{seconds:D2}";
        }
    }
}
