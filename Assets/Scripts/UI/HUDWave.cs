using Core;
using TMPro;
using UnityEngine;
using Waves;

namespace UI
{
    /// <summary>
    /// Drives the phase label and elapsed-time counter in the HUD. Subscribes
    /// to EventBus — no direct reference to WaveDirector except for the live
    /// elapsed-time readout, which has no event of its own (it changes every
    /// frame, so polling WaveDirector.ElapsedGameTime is simpler than an
    /// event firing every tick).
    ///
    /// Attach to: HUD_Canvas root. Wire TMP text refs in Inspector.
    /// </summary>
    public class HUDWave : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI phaseLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private WaveDirector waveDirector; // needed only for the live elapsed-time readout

        private void Start()
        {
            EventBus.Subscribe<PhaseStartedEvent>(OnPhaseStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PhaseStartedEvent>(OnPhaseStarted); }

        private void Update()
        {
            if (!timerLabel || !waveDirector) return;
            timerLabel.text = $"{waveDirector.ElapsedGameTime:F0}s";
        }

        private void OnPhaseStarted(PhaseStartedEvent phaseStarted)
        {
            if (!phaseLabel) return;

            phaseLabel.text = false // boss removed
                ? "BOSS"
                : $"{phaseStarted.EnemyType} incoming";
        }
        
    }
}
