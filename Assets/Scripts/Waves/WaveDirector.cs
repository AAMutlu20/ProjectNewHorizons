using System.Collections.Generic;
using Core;
using Difficulty;
using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Drives the infinite enemy-introduction cycle described in the design
    /// doc: phases unlock enemy types one at a time, but ALREADY-UNLOCKED
    /// types keep spawning -- per the doc, "spider enemy gets introduced
    /// into the mix" and "zombies increase as well, but slower." This is an
    /// ACCUMULATING set of concurrently-active spawn types, not a single
    /// "current phase" that gets replaced by the next one.
    ///
    /// Each EnemyCyclePhase's baseDuration means "how long after this phase
    /// unlocks until the NEXT phase unlocks" -- it does NOT mean "how long
    /// this type keeps spawning." Once unlocked, a type keeps spawning
    /// (on its own independent timer/count-ramp) until the boss phase
    /// clears the arena and the whole cycle restarts from just the first type.
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

        /// <summary>
        /// One concurrently-active spawning type. Each entry ticks its own
        /// spawnIntervalTimer and tracks its own timeSinceUnlocked (which
        /// drives that phase's countRampOverPhase curve) completely
        /// independently of every other active entry -- this is what lets
        /// Zombies and Spiders both keep spawning at once.
        /// </summary>
        private class ActivePhaseState
        {
            public int PhaseIndex;
            public float TimeSinceUnlocked;
            public float SpawnIntervalTimer;
        }

        private readonly List<ActivePhaseState> _activePhases = new();
        private readonly CycleEscalation _escalation = new();

        // Which phase index will unlock NEXT, and how long until it does --
        // separate from _activePhases, since "the next type is about to
        // unlock" is a distinct concept from "every type already unlocked
        // keeps spawning."
        private int _nextPhaseToUnlock;
        private float _timeUntilNextUnlock;

        private float _elapsedGameTime;
        private bool _isBossPhaseActive;
        private bool _isWaitingToStartNextCycle;
        private float _nextCycleDelayTimer;
        private bool _hasCycleStarted;

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
            Invoke(nameof(BeginCycle), delayBeforeFirstPhase);
        }

        private void BeginCycle()
        {
            _hasCycleStarted = true;
            ResetCycleState();
            UnlockNextPhase(); // unlocks phase 0 (Zombie) immediately
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;

            // Guards against ticking before BeginCycle's Invoke(delayBeforeFirstPhase)
            // has fired -- without this, Update runs from the very first frame,
            // before any phase has unlocked.
            if (!_hasCycleStarted) return;

            if (_isWaitingToStartNextCycle)
            {
                TickNextCycleDelay();
                return;
            }

            if (_isBossPhaseActive) return; // boss phase ends on EnemyDiedEvent, not a timer

            _currentDifficulty = difficultyScaler.Scale(_elapsedGameTime, _escalation);
            enemyPool.SetDifficulty(_currentDifficulty);

            TickActivePhaseSpawning();
            TickNextUnlockTimer();
        }

        private void ResetCycleState()
        {
            _activePhases.Clear();
            _nextPhaseToUnlock = 0;
            _timeUntilNextUnlock = 0f; // unlock phase 0 immediately on cycle start
        }

        /// <summary>
        /// Every currently-active phase spawns on its OWN independent timer
        /// and count-ramp -- this is the core of "accumulate, don't replace."
        /// </summary>
        private void TickActivePhaseSpawning()
        {
            foreach (var activePhase in _activePhases)
            {
                activePhase.TimeSinceUnlocked += Time.deltaTime;

                activePhase.SpawnIntervalTimer -= Time.deltaTime;
                if (activePhase.SpawnIntervalTimer > 0f) continue;

                var phase = cycleConfig.phases[activePhase.PhaseIndex];
                activePhase.SpawnIntervalTimer = phase.spawnInterval;

                var rampProgress = phase.baseDuration > 0f
                    ? Mathf.Clamp01(activePhase.TimeSinceUnlocked / phase.baseDuration)
                    : 1f;
                var spawnCountThisInterval = Mathf.Max(1, Mathf.RoundToInt(phase.countRampOverPhase.Evaluate(rampProgress)));

                for (var i = 0; i < spawnCountThisInterval; i++)
                    SpawnEnemy(phase.enemyType);
            }
        }

        /// <summary>
        /// Counts down to the next phase unlocking. baseDuration on the
        /// CURRENTLY-MOST-RECENTLY-UNLOCKED phase is what decides this delay --
        /// matching the doc's "a minute elapses, then the next type gets
        /// introduced" framing, while everything already unlocked keeps
        /// spawning via TickActivePhaseSpawning above, completely unaffected
        /// by this timer.
        /// </summary>
        private void TickNextUnlockTimer()
        {
            if (_nextPhaseToUnlock >= cycleConfig.phases.Length) return; // every non-boss phase already unlocked

            _timeUntilNextUnlock -= Time.deltaTime;
            if (_timeUntilNextUnlock > 0f) return;

            UnlockNextPhase();
        }

        private void UnlockNextPhase()
        {
            if (_nextPhaseToUnlock >= cycleConfig.phases.Length) return;

            var phaseIndex = _nextPhaseToUnlock;
            var phase = cycleConfig.phases[phaseIndex];
            _nextPhaseToUnlock++;

            if (phase.isBossPhase)
            {
                BeginBossPhase();
                return;
            }

            var effectiveDuration = Mathf.Max(1f, phase.baseDuration - _escalation.PhaseIntervalReduction);
            _timeUntilNextUnlock = effectiveDuration;

            _activePhases.Add(new ActivePhaseState { PhaseIndex = phaseIndex, TimeSinceUnlocked = 0f, SpawnIntervalTimer = 0f });

            SpawnInitialBurst(phase);

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: phase {phaseIndex} unlocked — {phase.enemyType} joins the mix " +
                          $"({_activePhases.Count} type(s) now active), {phase.startingCount} initial, " +
                          $"bossKills={_escalation.BossKillCount}");

            EventBus.Emit(new PhaseStartedEvent { EnemyType = phase.enemyType, PhaseIndex = phaseIndex });
        }

        private void SpawnInitialBurst(EnemyCyclePhase phase)
        {
            for (var i = 0; i < phase.startingCount; i++)
                SpawnEnemy(phase.enemyType);
        }

        private void SpawnEnemy(EnemyType enemyType)
        {
            // RollCandidatePosition is also the RETRY function -- if EnemyPool's
            // SpawnPositionResolver finds the first roll obstructed or ungrounded,
            // it calls this again for a fresh candidate on the same ring.
            Vector3 RollCandidatePosition() => SpawnShape.GetPosition(
                SpawnShapeType.Ring,
                GetArenaCentre(),
                _currentDifficulty.SpawnRadius,
                GetPlayerPosition());

            var isMiniboss = Random.value < _escalation.MinibossChance(cycleConfig.baseMinibossChance);
            enemyPool.Get(enemyType, RollCandidatePosition(), RollCandidatePosition, isMiniboss);
        }

        // Boss phase

        private void BeginBossPhase()
        {
            _isBossPhaseActive = true;
            enemyPool.ReturnAll(); // clear the arena per the design doc

            // The boss's candidate is always the arena centre, per the design doc --
            // but retrying the SAME point if obstructed would loop uselessly, so a
            // small jitter gives the resolver an actually different candidate.
            const float bossRetryJitterRadius = 2f;
            Vector3 RollBossRetryPosition()
            {
                var jitter2D = Random.insideUnitCircle * bossRetryJitterRadius;
                // Explicit X/Z construction -- Unity's implicit Vector2->Vector3 cast
                // would put jitter2D.y into world Y (height), not Z (ground depth).
                return GetArenaCentre() + new Vector3(jitter2D.x, 0f, jitter2D.y);
            }

            enemyPool.Get(EnemyType.Boss, GetArenaCentre(), RollBossRetryPosition);

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: boss phase started (kill #{_escalation.BossKillCount + 1})");

            EventBus.Emit(new PhaseStartedEvent { EnemyType = EnemyType.Boss, PhaseIndex = _nextPhaseToUnlock - 1 });
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
            ResetCycleState();
            UnlockNextPhase(); // restarts from phase 0 (Zombie) -- the whole accumulated set clears with the arena
        }

        // Helpers

        private Vector3 GetArenaCentre() => arenaCentre ? arenaCentre.position : Vector3.zero;
        private Vector3 GetPlayerPosition() => player ? player.position : Vector3.zero;
    }

    /// <summary>Emitted whenever a new phase unlocks — UI can use this to show "Spiders incoming!" etc.</summary>
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
