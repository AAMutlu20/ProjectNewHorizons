using Core;
using Difficulty;
using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Drives the infinite enemy-introduction cycle described in the design
    /// doc: phases unlock enemy types one at a time, each ramping in count
    /// over its duration, ending in a boss fight. After the boss dies, the
    /// cycle repeats from phase 0 — but harder, via CycleEscalation.
    ///
    /// Replaces the old finite WaveManager/WaveConfig[] entirely — there is
    /// no "wave index" here, only elapsed time and how many bosses have died.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [Header("Cycle data")]
        [SerializeField] private CycleConfigSo cycleConfig;

        [Header("Systems")]
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private DifficultyScaler difficultyScaler;

        [Header("Arena")]
        [SerializeField] private Transform arenaCentre;
        [SerializeField] private Transform player;

        [Header("Debug")]
        [SerializeField] private bool logPhaseEvents = true;

        [Header("Startup")]
        [SerializeField] private float delayBeforeFirstPhase = 2f;

        private readonly CycleEscalation _escalation = new();

        private int _currentPhaseIndex = -1;
        private float _phaseTimer;
        private float _elapsedGameTime;
        private float _spawnIntervalTimer;
        private bool _isBossPhaseActive;
        private bool _isWaitingToStartNextCycle;
        private float _nextCycleDelayTimer;

        private DifficultyParams _currentDifficulty;

        public float ElapsedGameTime => _elapsedGameTime;
        public int BossKillCount => _escalation.BossKillCount;

        private void Awake()
        {
            Debug.Assert(cycleConfig, "WaveDirector: CycleConfig not assigned.", this);
            Debug.Assert(enemyPool, "WaveDirector: EnemyPool not assigned.", this);
            Debug.Assert(difficultyScaler, "WaveDirector: DifficultyScaler not assigned.", this);
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            Invoke(nameof(BeginFirstPhase), delayBeforeFirstPhase);
        }

        private void BeginFirstPhase() => BeginPhase(0);

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;

            if (_isWaitingToStartNextCycle)
            {
                TickNextCycleDelay();
                return;
            }

            if (_isBossPhaseActive) return; // boss phase ends on EnemyDiedEvent, not a timer

            TickCurrentPhase();
        }

        // Phase lifecycle

        private void BeginPhase(int phaseIndex)
        {
            if (phaseIndex >= cycleConfig.phases.Length)
            {
                BeginPhase(0); // shouldn't normally happen — boss phase loops explicitly — but fail safe
                return;
            }

            _currentPhaseIndex = phaseIndex;
            var phase = cycleConfig.phases[phaseIndex];

            _currentDifficulty = difficultyScaler.Scale(_elapsedGameTime, _escalation);
            enemyPool.SetDifficulty(_currentDifficulty);

            if (phase.isBossPhase)
            {
                BeginBossPhase();
                return;
            }

            _phaseTimer = 0f;
            _spawnIntervalTimer = 0f;
            SpawnInitialBurst(phase);

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: phase {phaseIndex} started — {phase.enemyType}, " +
                          $"{phase.startingCount} initial, bossKills={_escalation.BossKillCount}");

            EventBus.Emit(new PhaseStartedEvent { EnemyType = phase.enemyType, PhaseIndex = phaseIndex });
        }

        private void TickCurrentPhase()
        {
            var phase = cycleConfig.phases[_currentPhaseIndex];

            _phaseTimer += Time.deltaTime;
            TickPhaseSpawning(phase);

            var effectiveDuration = Mathf.Max(1f, phase.baseDuration - _escalation.PhaseIntervalReduction);
            if (_phaseTimer >= effectiveDuration)
                BeginPhase(_currentPhaseIndex + 1);
        }

        private void TickPhaseSpawning(EnemyCyclePhase phase)
        {
            _spawnIntervalTimer -= Time.deltaTime;
            if (_spawnIntervalTimer > 0f) return;

            _spawnIntervalTimer = phase.spawnInterval;

            var phaseProgress = phase.baseDuration > 0f ? Mathf.Clamp01(_phaseTimer / phase.baseDuration) : 1f;
            var spawnCountThisInterval = Mathf.Max(1, Mathf.RoundToInt(phase.countRampOverPhase.Evaluate(phaseProgress)));

            for (var i = 0; i < spawnCountThisInterval; i++)
                SpawnEnemy(phase.enemyType);
        }

        private void SpawnInitialBurst(EnemyCyclePhase phase)
        {
            for (var i = 0; i < phase.startingCount; i++)
                SpawnEnemy(phase.enemyType);
        }

        private void SpawnEnemy(EnemyType enemyType)
        {
            _currentDifficulty = difficultyScaler.Scale(_elapsedGameTime, _escalation);
            enemyPool.SetDifficulty(_currentDifficulty);

            var spawnPosition = SpawnShape.GetPosition(
                SpawnShapeType.Ring,
                GetArenaCentre(),
                _currentDifficulty.SpawnRadius,
                GetPlayerPosition());

            var isMiniboss = Random.value < _escalation.MinibossChance(cycleConfig.baseMinibossChance);
            enemyPool.Get(enemyType, spawnPosition, isMiniboss);
        }

        // Boss phase

        private void BeginBossPhase()
        {
            _isBossPhaseActive = true;
            enemyPool.ReturnAll(); // clear the arena per the design doc

            var bossSpawnPosition = GetArenaCentre();
            enemyPool.Get(EnemyType.Boss, bossSpawnPosition);

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: boss phase started (kill #{_escalation.BossKillCount + 1})");

            EventBus.Emit(new PhaseStartedEvent { EnemyType = EnemyType.Boss, PhaseIndex = _currentPhaseIndex });
        }

        private void OnEnemyDied(EnemyDiedEvent enemyDied)
        {
            if (!enemyDied.IsBoss) return;
            if (!_isBossPhaseActive) return; // ignore stray boss-death events outside an active boss phase

            _isBossPhaseActive = false;
            _escalation.OnBossKilled();

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: boss killed — total kills now {_escalation.BossKillCount}");

            EventBus.Emit(new CycleCompleteEvent { BossKillCount = _escalation.BossKillCount });

            _isWaitingToStartNextCycle = true;
            _nextCycleDelayTimer = cycleConfig.delayAfterBossKill;
        }

        private void TickNextCycleDelay()
        {
            _nextCycleDelayTimer -= Time.deltaTime;
            if (_nextCycleDelayTimer > 0f) return;

            _isWaitingToStartNextCycle = false;
            BeginPhase(0);
        }

        // Helpers

        private Vector3 GetArenaCentre() => arenaCentre ? arenaCentre.position : Vector3.zero;
        private Vector3 GetPlayerPosition() => player ? player.position : Vector3.zero;
    }

    /// <summary>Emitted whenever a new phase begins — UI can use this to show "Spiders incoming!" etc.</summary>
    public struct PhaseStartedEvent
    {
        public EnemyType EnemyType;
        public int PhaseIndex;
    }

    /// <summary>Emitted when a boss dies and the cycle is about to restart, harder.</summary>
    public struct CycleCompleteEvent
    {
        public int BossKillCount;
    }
}
