using Abilities;
using Core;
using Enemies;
using Stats;
using System.Collections;
using UnityEngine;

namespace XP
{
    /// <summary>
    /// Tracks player XP and level. Listens for enemy deaths and boss kills.
    /// Per the design doc, regular/miniboss enemy deaths spawn an XP orb the
    /// player must walk near to collect (see XpOrbPool/XpOrb) rather than
    /// granting XP instantly -- only GrantPickedUpXp (called by XpOrb on
    /// pickup) actually adds to the player's total for those deaths. Boss
    /// kills are the one exception: XP is still granted instantly on kill,
    /// since a boss death is a bigger, more ceremonial moment that doesn't
    /// need a pickup step.
    ///
    /// Owns the freeze-and-unfreeze around a choice screen via GameFreezeController.
    ///
    /// Attach to: PlayerRoot or a dedicated [Systems] GameObject. Needs a
    /// StatSheet reference (for Experience Gain), an XpCurveConfigSo, a
    /// StatPoolSo, an AbilityRoster, and an XpOrbPool.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        private const float PercentToFraction = 100f;
        private const float UnfreezeDelaySeconds = 0.5f;
        private const int LevelsPerAbilityChoice = 3;
        private const string FreezeReason = "LevelUpChoice";

        [SerializeField] private StatSheet statSheet;
        [SerializeField] private XpCurveConfigSo xpCurveConfig;
        [SerializeField] private RarityWeightTableSo rarityWeights;
        [SerializeField] private StatPoolSo statPool;
        [SerializeField] private AbilityRoster abilityRoster;
        [SerializeField] private XpOrbPool xpOrbPool;

        private LevelUpChoiceGenerator _statChoiceGenerator;
        private AbilityChoiceGenerator _abilityChoiceGenerator;
        private float _currentXp;
        private int _currentLevel;
        private float _elapsedGameTime;
        // True while the current choice screen was triggered by a cycle end —
        // causes the existing resolve methods to also emit CycleRewardResolvedEvent.
        private bool _isCycleRewardPending;

        public int CurrentLevel => _currentLevel;

        private void Awake()
        {
            Debug.Assert(statSheet, "LevelSystem: StatSheet not assigned.", this);
            Debug.Assert(xpCurveConfig, "LevelSystem: XpCurveConfig not assigned.", this);
            Debug.Assert(rarityWeights, "LevelSystem: RarityWeightTable not assigned.", this);
            Debug.Assert(statPool, "LevelSystem: StatPool not assigned.", this);
            Debug.Assert(abilityRoster, "LevelSystem: AbilityRoster not assigned.", this);
            Debug.Assert(xpOrbPool, "LevelSystem: XpOrbPool not assigned.", this);

            _statChoiceGenerator = new LevelUpChoiceGenerator(statPool, rarityWeights);
            _abilityChoiceGenerator = new AbilityChoiceGenerator(abilityRoster, rarityWeights);
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<CycleEndedEvent>(OnCycleEnded);
            EmitExperienceChanged();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<CycleEndedEvent>(OnCycleEnded);
        }

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;
        }

        private void OnEnemyDied(EnemyDiedEvent enemyDied)
        {
            SpawnXpOrbForEnemy(enemyDied);
        }

        /// <summary>
        /// Resolves the time/miniboss-scaled XP value AT THE MOMENT OF DEATH
        /// (elapsed time matters here, not at whatever later moment the
        /// player actually picks the orb up) and spawns an orb carrying that
        /// already-resolved amount.
        /// </summary>
        private void SpawnXpOrbForEnemy(EnemyDiedEvent enemyDied)
        {
            var timeScaledXp = enemyDied.XpValue * xpCurveConfig.GetEnemyXpMultiplier(_elapsedGameTime);
            var minibossScaledXp = enemyDied.IsMiniboss
                ? timeScaledXp * xpCurveConfig.minibossXpMultiplier
                : timeScaledXp;

            xpOrbPool.Spawn(enemyDied.Position, minibossScaledXp);
        }

        /// <summary>
        /// Called by XpOrb when the player collects it. The orb already
        /// carries its fully time/miniboss-scaled value (resolved at the
        /// moment of death, see SpawnXpOrbForEnemy) -- this only applies the
        /// Experience Gain stat bonus, same as any other XP grant.
        /// </summary>
        public void GrantPickedUpXp(float orbXpValue)
        {
            GrantXp(orbXpValue);
        }

        private void GrantXp(float baseAmount)
        {
            var amountWithBonus = statSheet.ApplyPercentBonus(baseAmount, StatType.ExperienceGain);
            _currentXp += amountWithBonus;

            TryLevelUp();
            EmitExperienceChanged();
        }

        private void TryLevelUp()
        {
            var xpRequired = xpCurveConfig.GetXpRequiredForLevel(_currentLevel + 1);
            if (_currentXp < xpRequired) return;

            _currentXp -= xpRequired;
            _currentLevel++;

            BeginLevelUpChoice();
        }

        private void BeginLevelUpChoice()
        {
            GameFreezeController.RequestFreeze(FreezeReason);

            var isAbilityLevel = _currentLevel % LevelsPerAbilityChoice == 0;
            EventBus.Emit(new LevelUpEvent { NewLevel = _currentLevel, IsAbilityLevel = isAbilityLevel });

            if (isAbilityLevel)
            {
                var abilityChoices = _abilityChoiceGenerator.RollLevelUpChoices();
                EventBus.Emit(new AbilityChoicePresentedEvent { Choices = abilityChoices });
            }
            else
            {
                var statChoices = _statChoiceGenerator.RollLevelUpChoices();
                EventBus.Emit(new StatChoicePresentedEvent { Choices = statChoices });
            }
        }

        // ── Cycle-end Legendary reward ────────────────────────────────────────

        private void OnCycleEnded(CycleEndedEvent e)
        {
            _isCycleRewardPending = true;
            BeginLegendaryChoice();
        }

        /// <summary>
        /// Presents a Legendary-inclusive choice using the SAME UI as a normal
        /// level-up — same events, same screens, same card widgets. The only
        /// difference is RollAnyRarity instead of RollExcludingLegendary, and
        /// the _isCycleRewardPending flag that makes the resolve methods also
        /// emit CycleRewardResolvedEvent so WaveDirector knows to restart.
        /// </summary>
        private void BeginLegendaryChoice()
        {
            GameFreezeController.RequestFreeze(FreezeReason);

            var isAbilityLevel = _currentLevel % LevelsPerAbilityChoice == 0;
            EventBus.Emit(new LevelUpEvent { NewLevel = _currentLevel, IsAbilityLevel = isAbilityLevel });

            if (isAbilityLevel)
            {
                var choices = _abilityChoiceGenerator.RollLevelUpChoicesLegendary();
                EventBus.Emit(new AbilityChoicePresentedEvent { Choices = choices });
            }
            else
            {
                var choices = _statChoiceGenerator.RollLevelUpChoicesLegendary();
                EventBus.Emit(new StatChoicePresentedEvent { Choices = choices });
            }
        }

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the choice UI once the player picks a stat card. Applies
        /// the stat and unfreezes the game after a short delay so the player
        /// has a beat to re-orient before gameplay resumes.
        /// </summary>
        public void ResolveStatChoice(StatModifier chosenModifier)
        {
            statSheet.ApplyModifier(chosenModifier);
            EventBus.Emit(new StatChoiceResolvedEvent { ChosenModifier = chosenModifier });

            if (_isCycleRewardPending)
            {
                _isCycleRewardPending = false;
                EventBus.Emit(new CycleRewardResolvedEvent());
            }

            StartCoroutine(UnfreezeAfterDelay());
        }

        /// <summary>Called by the choice UI once the player picks an ability card. Mirrors ResolveStatChoice.</summary>
        public void ResolveAbilityChoice(AbilityChoiceOption chosenOption)
        {
            chosenOption.Entry.Grant(chosenOption.Rarity);
            EventBus.Emit(new AbilityChoiceResolvedEvent { ChosenOption = chosenOption });

            if (_isCycleRewardPending)
            {
                _isCycleRewardPending = false;
                EventBus.Emit(new CycleRewardResolvedEvent());
            }

            StartCoroutine(UnfreezeAfterDelay());
        }

        /// <summary>
        /// Waits UnfreezeDelaySeconds of REAL (unscaled) time before releasing
        /// the freeze. This used to be Invoke(nameof(Unfreeze), ...), which is
        /// a genuine deadlock: Invoke's delay is scaled by Time.timeScale, and
        /// since RequestFreeze sets timeScale to 0, that delay could never
        /// elapse -- the game would freeze on every level-up and never recover.
        /// WaitForSecondsRealtime ignores timeScale entirely, so it actually fires.
        /// </summary>
        private IEnumerator UnfreezeAfterDelay()
        {
            yield return new WaitForSecondsRealtime(UnfreezeDelaySeconds);
            GameFreezeController.ReleaseFreeze(FreezeReason);
        }

        private void EmitExperienceChanged()
        {
            EventBus.Emit(new ExperienceChangedEvent
            {
                CurrentXp = _currentXp,
                XpToNextLevel = xpCurveConfig.GetXpRequiredForLevel(_currentLevel + 1),
                Level = _currentLevel,
            });
        }
    }
}
