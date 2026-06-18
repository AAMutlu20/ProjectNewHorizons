using Difficulty;
using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Consumes a WaveConfig + DifficultyParams and fires enemy spawn requests
    /// at the correct times. Pure C# class — no MonoBehaviour, no allocation in Tick().
    ///
    /// Owned by WaveManager (not a separate scene object).
    /// </summary>
    public class SpawnScheduler
    {
        private WaveConfig _config;
        private DifficultyParams _diff;
        private readonly EnemyPool _pool;
        private int[] _spawnedPerGroup; // how many we've already spawned from each group
        private int[] _totalPerGroup; // target count per group (base × countMult)

        public bool IsExhausted { get; private set; }

        // Setup
        public SpawnScheduler(EnemyPool pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// Load a new wave. Call this before the first Tick() of the wave.
        /// </summary>
        public void LoadWave(WaveConfig config, DifficultyParams diff)
        {
            _config  = config;
            _diff = diff;
            IsExhausted = false;

            _spawnedPerGroup = new int[config.groups.Length];
            _totalPerGroup = new int[config.groups.Length];

            for (var g = 0; g < config.groups.Length; g++)
            {
                // Apply count multiplier and round up — always spawn at least 1
                _totalPerGroup[g] = Mathf.Max(1,
                    Mathf.RoundToInt(config.groups[g].count * diff.CountMult));
            }

            _pool.SetDifficulty(diff);
        }

        /// <summary>
        /// Called every frame by WaveManager. waveTime is seconds since wave start.
        /// Spawns any enemies that are due by now.
        /// </summary>
        public void Tick(float waveTime, Vector3 arenaCentre, Vector3 playerPos)
        {
            if (IsExhausted || !_config) return;

            var allDone = true;

            for (var g = 0; g < _config.groups.Length; g++)
            {
                var group = _config.groups[g];
                var target = _totalPerGroup[g];
                var spawned = _spawnedPerGroup[g];

                if (spawned >= target) continue; // this group is done

                allDone = false;

                var localTime = waveTime - group.startTime;
                if (localTime < 0f) continue;   // group hasn't started yet

                // How many should have spawned by now, capped at target
                var shouldHaveSpawned = Mathf.Min(
                    Mathf.FloorToInt(localTime / group.interval) + 1,
                    target);

                var toSpawnNow = shouldHaveSpawned - spawned;
                if (toSpawnNow <= 0) continue;

                for (var i = 0; i < toSpawnNow; i++)
                {
                    var pos = SpawnShape.GetPosition(
                        group.spawnShape,
                        arenaCentre,
                        _diff.SpawnRadius,
                        playerPos,
                        group.arcAngle,
                        group.arcDirection);

                    var view = _pool.Get(group.enemyType, pos);
                    // view can be null if pool is exhausted or budget is full — that's fine,
                    // it'll try again next frame. Don't increment spawnedPerGroup.
                    if (view)
                        _spawnedPerGroup[g]++;
                }
            }

            if (allDone)
                IsExhausted = true;
        }
    }
}
