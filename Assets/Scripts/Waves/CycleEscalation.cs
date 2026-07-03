namespace Waves
{
    /// <summary>
    /// Tracks per-cycle escalation. Incremented once per completed cycle
    /// (i.e. every time the TornadoGhost phase completes and the cycle
    /// restarts). Drives miniboss frequency and phase interval compression.
    /// </summary>
    public class CycleEscalation
    {
        private const float PhaseIntervalReductionPerCycle = 5f;
        private const float MinibossChanceIncreasePerCycle = 0.05f;

        /// <summary>Phase intervals shrink per cycle but floor at 30s.</summary>
        public const float MinPhaseIntervalSeconds = 30f;

        public int CycleCount { get; private set; }

        /// <summary>Called by WaveDirector each time a full cycle completes.</summary>
        public void OnCycleCompleted() => CycleCount++;

        /// <summary>
        /// Stat bonus multiplier — enemies get 15% stronger per completed cycle,
        /// stacking multiplicatively on top of time-based difficulty scaling.
        /// </summary>
        public float StatBonusMultiplier => UnityEngine.Mathf.Pow(1.15f, CycleCount);

        /// <summary>Seconds to subtract from every phase's base interval per cycle.</summary>
        public float PhaseIntervalReduction => CycleCount * PhaseIntervalReductionPerCycle;

        /// <summary>
        /// Miniboss chance increases with both elapsed time and cycle count.
        /// Time contribution: +0.5% per minute. Cycle contribution: +5% per cycle.
        /// Capped at 25%.
        /// </summary>
        public float MinibossChance(float baseChance, float elapsedGameTimeSeconds)
        {
            var minutesElapsed = elapsedGameTimeSeconds / 60f;
            return UnityEngine.Mathf.Clamp(
                baseChance + minutesElapsed * 0.005f + CycleCount * MinibossChanceIncreasePerCycle,
                0f, 0.25f);
        }
    }
}
