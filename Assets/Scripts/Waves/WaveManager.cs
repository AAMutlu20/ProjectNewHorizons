using Core;
using Difficulty;
using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Orchestrates the wave lifecycle:
    ///   StartWave → SpawnScheduler.LoadWave → Tick → WaveComplete → rest → next wave
    ///
    /// Attach to: a GameObject in [Systems].
    /// Wire: all references in Inspector.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave data")]
        [SerializeField] private WaveConfig[] waveConfigs;

        [Header("Systems")]
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private DifficultyScaler difficultyScaler;

        [Header("Arena")]
        [SerializeField] private Transform arenaCenter;   // (0,0,0) duh
        [SerializeField] private Transform player;

        [Header("Debug")]
        [SerializeField] private bool autoAdvanceWaves = true;
        [SerializeField] private bool logWaveEvents    = true;

        //State
        private int CurrentWave { get; set; } = -1;
        public float WaveTimer { get; private set; }
        public bool  IsWaveActive { get; private set; }
        public bool  IsResting { get; private set; }

        private float _restTimer;
        private SpawnScheduler _scheduler;
        private bool _allWavesDone;

        //Lifecycle

        private void Awake()
        {
            Debug.Assert(waveConfigs is { Length: > 0 },
                "WaveManager: no WaveConfigs assigned");
            Debug.Assert(enemyPool, "WaveManager: EnemyPool not assigned");
            Debug.Assert(difficultyScaler, "WaveManager: DifficultyScaler not assigned");

            _scheduler = new SpawnScheduler(enemyPool);
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyReturnedEvent>(OnEnemyReturned);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyReturnedEvent>(OnEnemyReturned);
        }

        private void Update()
        {
            if (_allWavesDone) return;

            // Rest between waves
            if (IsResting)
            {
                _restTimer -= Time.deltaTime;
                if (!(_restTimer <= 0f)) return;
                IsResting = false;
                if (autoAdvanceWaves)
                    StartWave(CurrentWave + 1);
                return;
            }

            if (!IsWaveActive) return;

            // Active wave
            WaveTimer += Time.deltaTime;

            var centre = arenaCenter ? (Vector2)arenaCenter.position : Vector2.zero;
            var playerPos = player ? (Vector2)player.position : Vector2.zero;

            _scheduler.Tick(WaveTimer, centre, playerPos);

            // Check time limit
            var cfg = waveConfigs[CurrentWave];
            var timeLimitReached = cfg.waveDuration > 0f && WaveTimer >= cfg.waveDuration;

            // Wave ends when: scheduler exhausted AND no active enemies, OR time limit hit
            var waveOver = (_scheduler.IsExhausted && enemyPool.ActiveCount == 0)
                           || timeLimitReached;

            if (waveOver)
                CompleteWave();
        }

        // Public API

        /// <summary>Start a specific wave by index. Safe to call from GameManager or debug UI.</summary>
        public void StartWave(int index)
        {
            if (index >= waveConfigs.Length)
            {
                if (logWaveEvents) Debug.Log("WaveManager: all waves complete!");
                _allWavesDone = true;
                return;
            }

            if (index < 0) index = 0;

            CurrentWave  = index;
            WaveTimer    = 0f;
            IsWaveActive = true;
            IsResting    = false;

            DifficultyParams diff = difficultyScaler.Scale(CurrentWave);
            _scheduler.LoadWave(waveConfigs[CurrentWave], diff);

            if (logWaveEvents)
                Debug.Log($"WaveManager: Wave {CurrentWave + 1} started " +
                          $"(countMult={diff.CountMult:F1}, speedMult={diff.SpeedMult:F1})");

            EventBus.Emit(new WaveStartedEvent
            {
                Wave          = CurrentWave,
                WaveStartTime = Time.time,
            });
        }

        /// <summary>Force-skip to the next wave. Useful for debug or designer testing.</summary>
        private void SkipWave()
        {
            enemyPool.ReturnAll();
            CompleteWave();
        }

        // Internals

        private void CompleteWave()
        {
            if (!IsWaveActive) return;

            IsWaveActive = false;

            if (logWaveEvents)
                Debug.Log($"WaveManager: Wave {CurrentWave + 1} complete in {WaveTimer:F1}s");

            EventBus.Emit(new WaveCompleteEvent
            {
                Wave     = CurrentWave,
                Duration = WaveTimer,
            });

            // Start rest timer before next wave
            var rest = waveConfigs[CurrentWave].restDuration;
            if (rest > 0f)
            {
                IsResting  = true;
                _restTimer = rest;
            }
            else if (autoAdvanceWaves)
            {
                StartWave(CurrentWave + 1);
            }
        }

        private static void OnEnemyReturned(EnemyReturnedEvent _)
        {
            // EnemyPool.Return() fires this — check wave-end condition each frame in Update,
            // so this is just a hook if I need instant reaction (e.g. playing a sound on last kill)
        }

        // Editor helpers
#if UNITY_EDITOR
        [ContextMenu("Debug: Skip Current Wave")]
        private void DebugSkipWave() => SkipWave();

        [ContextMenu("Debug: Start Wave 0")]
        private void DebugRestartWaves() => StartWave(0);
#endif
    }
}
