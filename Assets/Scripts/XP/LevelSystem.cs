using Core;
using Stats;
using UnityEngine;

namespace XP
{
    /// <summary>
    /// Tracks player XP and level. Listens for enemy deaths and boss kills,
    /// grants XP (scaled by Experience Gain stat), and triggers level-up /
    /// ability-choice flows. Owns the freeze-and-unfreeze around a choice
    /// screen via Time.timeScale.
    ///
    /// Attach to: PlayerRoot or a dedicated [Systems] GameObject. Needs a
    /// StatSheet reference (for Experience Gain) and an XpCurveConfigSo.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        private const float PercentToFraction = 100f;
        private const float UnfreezeDelaySeconds = 0.5f;
        private const int LevelsPerAbilityChoice = 3;

        [SerializeField] private StatSheet statSheet;
        [SerializeField] private XpCurveConfigSo xpCurveConfig;
        [SerializeField] private RarityWeightTableSo rarityWeights;
        [SerializeField] private StatPoolSo statPool;

        private LevelUpChoiceGenerator _choiceGenerator;
        private float _currentXp;
        private int _currentLevel;
        private int _bossKillCount;
        private float _elapsedGameTime;

        public int CurrentLevel => _currentLevel;

        private void Awake()
        {
            Debug.Assert(statSheet, "LevelSystem: StatSheet not assigned.", this);
            Debug.Assert(xpCurveConfig, "LevelSystem: XpCurveConfig not assigned.", this);
            Debug.Assert(rarityWeights, "LevelSystem: RarityWeightTable not assigned.", this);
            Debug.Assert(statPool, "LevelSystem: StatPool not assigned.", this);

            _choiceGenerator = new LevelUpChoiceGenerator(statPool, rarityWeights);
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EmitExperienceChanged();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void Update()
        {
            _elapsedGameTime += Time.deltaTime;
        }

        private void OnEnemyDied(EnemyDiedEvent enemyDied)
        {
            if (enemyDied.IsBoss)
            {
                GrantBossXp();
                return;
            }

            GrantEnemyXp(enemyDied);
        }

        private void GrantEnemyXp(EnemyDiedEvent enemyDied)
        {
            var timeScaledXp = enemyDied.XpValue * xpCurveConfig.GetEnemyXpMultiplier(_elapsedGameTime);
            var minibossScaledXp = enemyDied.IsMiniboss
                ? timeScaledXp * xpCurveConfig.minibossXpMultiplier
                : timeScaledXp;

            GrantXp(minibossScaledXp);
        }

        private void GrantBossXp()
        {
            var xpRequiredForNextLevel = xpCurveConfig.GetXpRequiredForLevel(_currentLevel + 1);
            var bossXpFraction = xpCurveConfig.GetBossXpFraction(_bossKillCount);

            _bossKillCount++;
            GrantXp(xpRequiredForNextLevel * bossXpFraction);
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
            Time.timeScale = 0f;

            var isAbilityLevel = _currentLevel % LevelsPerAbilityChoice == 0;
            EventBus.Emit(new LevelUpEvent { NewLevel = _currentLevel, IsAbilityLevel = isAbilityLevel });

            if (!isAbilityLevel)
            {
                var choices = _choiceGenerator.RollLevelUpChoices();
                EventBus.Emit(new StatChoicePresentedEvent { Choices = choices });
            }

            // Ability choice presentation is owned by the future Abilities system —
            // LevelUpEvent.IsAbilityLevel is the signal it listens for.
        }

        /// <summary>
        /// Called by the choice UI once the player picks a card. Applies the
        /// stat and unfreezes the game after a short delay so the player has
        /// a beat to re-orient before gameplay resumes.
        /// </summary>
        public void ResolveStatChoice(StatModifier chosenModifier)
        {
            statSheet.ApplyModifier(chosenModifier);
            EventBus.Emit(new StatChoiceResolvedEvent { ChosenModifier = chosenModifier });

            Invoke(nameof(Unfreeze), UnfreezeDelaySeconds);
        }

        private void Unfreeze()
        {
            Time.timeScale = 1f;
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
