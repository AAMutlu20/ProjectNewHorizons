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

        [Tooltip("Reads the live camera FOV and position to calculate the minimum inner radius " +
                 "that guarantees spawns are off-screen at the current zoom level. " +
                 "Attach SpawnRadiusProvider to the CinemachineCamera GameObject and wire it here. " +
                 "If left null, falls back to spawnInnerRadiusFallback.")]
        [SerializeField] private Camera.SpawnRadiusProvider spawnRadiusProvider;

        [Tooltip("Used only when spawnRadiusProvider is not assigned — static fallback. " +
                 "Set to roughly your camera's half-diagonal at max zoom-out.")]
        [SerializeField] private float spawnInnerRadiusFallback = 10f;

        [Header("Debug")]
        [SerializeField] private bool logPhaseEvents = true;

        [Header("Startup")]
        [SerializeField] private float delayBeforeFirstPhase = 2f;

        [Header("Sequential spawning")]
        [Tooltip("Max enemies to activate from the burst queue per frame — design doc Section 17.1: " +
                 "spreading burst activations across frames prevents a spike on the frame a new phase " +
                 "unlocks. 2-3 per frame at 60fps is imperceptible to players.")]
        [SerializeField] private int burstActivationsPerFrame = 3;

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

        // Sequential burst queue — SpawnInitialBurst and TickActivePhaseSpawning enqueue here
        // instead of calling SpawnEnemy directly, so the per-frame drain limit applies.
        // Design doc Section 17.1: burst activations spread across frames to eliminate the
        // single-frame CPU spike that all-at-once Awake/activation otherwise causes.
        private readonly Queue<EnemyType> _burstQueue = new();

        // Which phase index will unlock NEXT, and how long until it does --
        // separate from _activePhases, since "the next type is about to
        // unlock" is a distinct concept from "every type already unlocked
        // keeps spawning."
        private int _nextPhaseToUnlock;
        private float _timeUntilNextUnlock;

        private float _elapsedGameTime;
        private bool _hasCycleStarted;

        // Set to true when all phases have completed their baseDuration —
        // spawning pauses while the player picks their Legendary reward.
        // Cleared by RestartCycle() once the reward is resolved.
        private bool _isWaitingForCycleReward;
        private int _cycleNumber; // 1-indexed count of full cycles completed

        private DifficultyParams _currentDifficulty;

        public float ElapsedGameTime => _elapsedGameTime;
        private void Awake()
        {
            Debug.Assert(cycleConfig, "WaveDirector: CycleConfig not assigned.", this);
            Debug.Assert(enemyPool, "WaveDirector: EnemyPool not assigned.", this);
            Debug.Assert(difficultyScaler, "WaveDirector: DifficultyScaler not assigned.", this);
        }

        private void Start()
        {
            EventBus.Subscribe<CycleRewardResolvedEvent>(OnCycleRewardResolved);
            Invoke(nameof(BeginCycle), delayBeforeFirstPhase);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CycleRewardResolvedEvent>(OnCycleRewardResolved);
        }

        private void BeginCycle()
        {
            _hasCycleStarted = true;
            ResetCycleState();
            UnlockNextPhase(); // unlocks phase 0 (Zombie) immediately
        }

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;

            if (!_hasCycleStarted) return;
            if (_isWaitingForCycleReward) return; // paused while player picks Legendary reward

            _currentDifficulty = difficultyScaler.Scale(_elapsedGameTime, _escalation);
            enemyPool.SetDifficulty(_currentDifficulty);

            DrainBurstQueue();
            TickActivePhaseSpawning();
            TickNextUnlockTimer();
        }

        /// <summary>
        /// Drains up to burstActivationsPerFrame entries from the burst queue each frame.
        /// Each dequeued entry results in one SpawnEnemy call — sequential, not batch.
        /// </summary>
        private void DrainBurstQueue()
        {
            var drained = 0;
            while (_burstQueue.Count > 0 && drained < burstActivationsPerFrame)
            {
                var enemyType = _burstQueue.Dequeue();
                SpawnEnemy(enemyType);
                drained++;
            }
        }

        private void ResetCycleState()
        {
            _activePhases.Clear();
            _burstQueue.Clear(); // discard any pending activations from the previous cycle
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

                // Enqueue rather than spawn directly — DrainBurstQueue spreads these
                // across frames (burstActivationsPerFrame per frame) so the interval
                // tick doesn't cause a frame spike when multiple types fire at once.
                for (var i = 0; i < spawnCountThisInterval; i++)
                    _burstQueue.Enqueue(phase.enemyType);
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
            if (_nextPhaseToUnlock >= cycleConfig.phases.Length)
            {
                // All phases are active. Wait for the LAST phase to finish its
                // baseDuration, then end the cycle and give the Legendary reward.
                // We track this by checking the last ActivePhaseState's TimeSinceUnlocked.
                if (_activePhases.Count == 0) return;
                var lastPhase = _activePhases[_activePhases.Count - 1];
                var lastPhaseDef = cycleConfig.phases[lastPhase.PhaseIndex];
                if (lastPhase.TimeSinceUnlocked >= lastPhaseDef.baseDuration)
                    BeginCycleEnd();
                return;
            }

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

            // Phase intervals are floored at MinPhaseIntervalSeconds —
            // confirmed design doc floor (Section 9.5 / CycleEscalation.MinPhaseIntervalSeconds).
            var effectiveDuration = Mathf.Max(
                CycleEscalation.MinPhaseIntervalSeconds,
                phase.baseDuration - _escalation.PhaseIntervalReduction);
            _timeUntilNextUnlock = effectiveDuration;

            _activePhases.Add(new ActivePhaseState { PhaseIndex = phaseIndex, TimeSinceUnlocked = 0f, SpawnIntervalTimer = 0f });

            SpawnInitialBurst(phase);

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: phase {phaseIndex} unlocked — {phase.enemyType} joins the mix " +
                          $"({_activePhases.Count} type(s) now active), {phase.startingCount} initial, ");

            EventBus.Emit(new PhaseStartedEvent { EnemyType = phase.enemyType, PhaseIndex = phaseIndex });
        }

        private void SpawnInitialBurst(EnemyCyclePhase phase)
        {
            // Enqueue the whole burst — DrainBurstQueue drains burstActivationsPerFrame
            // per frame so the phase-unlock moment doesn't activate all N instances at once.
            // Design doc Section 17.1: sequential spawning for burst events.
            for (var i = 0; i < phase.startingCount; i++)
                _burstQueue.Enqueue(phase.enemyType);
        }

        private void SpawnEnemy(EnemyType enemyType)
        {
            var innerRadius = spawnRadiusProvider
                ? spawnRadiusProvider.SafeInnerRadius
                : spawnInnerRadiusFallback;

            // Ring centred on player so spawns are always just outside the camera.
            Vector3 Roll() => SpawnShape.GetPosition(
                SpawnShapeType.Ring,
                GetPlayerPosition(),
                _currentDifficulty.SpawnRadius,
                innerRadius: innerRadius);

            var so = enemyPool.FindSoPublic(enemyType);
            var isMiniboss = so != null && so.canBeMiniboss
                && Random.value < _escalation.MinibossChance(cycleConfig.baseMinibossChance, _elapsedGameTime);

            enemyPool.Get(enemyType, Roll(), Roll, isMiniboss);
        }

        // Cycle end and restart

        private void BeginCycleEnd()
        {
            if (_isWaitingForCycleReward) return; // guard against firing multiple times
            _isWaitingForCycleReward = true;
            _cycleNumber++;

            // Clear the arena — all active enemies despawn instantly.
            enemyPool.ReturnAll();
            _burstQueue.Clear();

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: cycle {_cycleNumber} complete — presenting Legendary reward.");

            // LevelSystem listens to this and presents the Legendary choice screen.
            // Spawning is paused (_isWaitingForCycleReward = true) until the player
            // picks and CycleRewardResolvedEvent fires back.
            EventBus.Emit(new CycleEndedEvent { CycleNumber = _cycleNumber });
        }

        private void OnCycleRewardResolved(CycleRewardResolvedEvent _)
        {
            // Player has picked their Legendary reward. Wait the configured delay
            // then restart the cycle, escalated.
            Invoke(nameof(RestartCycle), cycleConfig.delayAfterCycleEnd);
        }

        private void RestartCycle()
        {
            _escalation.OnCycleCompleted();
            _isWaitingForCycleReward = false;
            ResetCycleState();
            UnlockNextPhase(); // restarts from Zombie

            if (logPhaseEvents)
                Debug.Log($"WaveDirector: cycle restarting (escalation cycle #{_escalation.CycleCount}).");
        }

        // Helpers

        private Vector3 GetPlayerPosition() => player ? player.position : Vector3.zero;
    }

    /// <summary>Emitted whenever a new phase unlocks — UI can use this to show "Spiders incoming!" etc.</summary>
    public struct PhaseStartedEvent
    {
        public EnemyType EnemyType;
        public int PhaseIndex;
    }
}
