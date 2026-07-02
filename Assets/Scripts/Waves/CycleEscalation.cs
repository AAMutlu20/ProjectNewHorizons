namespace Waves
{
    /// <summary>
    /// Tracks how much harder the enemy-introduction cycle has become after
    /// repeated boss kills. Per the design doc: after every boss, enemies get
    /// a flat 15% stronger (on top of normal time-based scaling), their
    /// phases introduce 5 seconds faster, and minibosses appear more often.
    ///
    /// Pure data/math — no MonoBehaviour. Owned by WaveDirector, incremented
    /// once per boss kill.
    /// </summary>
    public class CycleEscalation
    {
        private const float StatBonusPerBossKill = 0.15f;
        private const float PhaseIntervalReductionPerBossKill = 5f;
        private const float MinibossChanceIncreasePerBossKill = 0.05f;

        public int BossKillCount { get; private set; }

        /// <summary>Flat stat multiplier bonus stacked on top of normal time-based scaling.</summary>
        public float StatBonusMultiplier => 1f + BossKillCount * StatBonusPerBossKill;

        /// <summary>Seconds to subtract from every phase's base interval (e.g. 60s → 55s after one boss).</summary>
        public float PhaseIntervalReduction => BossKillCount * PhaseIntervalReductionPerBossKill;

        /// <summary>Chance (0..1) that an eligible spawn becomes a miniboss, increasing with each boss kill.</summary>
        public float MinibossChance(float baseChance) =>
            UnityEngine.Mathf.Clamp01(baseChance + BossKillCount * MinibossChanceIncreasePerBossKill);

        public void OnBossKilled()
        {
            BossKillCount++;
        }
    }
}
