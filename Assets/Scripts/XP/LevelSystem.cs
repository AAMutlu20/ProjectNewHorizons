using Abilities;
using Core;
using Enemies;
using Stats;
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
        private int _bossKillCount;
        private float _elapsedGameTime;

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

        private void GrantBossXp()
        {
            var xpRequiredForNextLevel = xpCurveConfig.GetXpRequiredForLevel(_currentLevel + 1);
            var bossXpFraction = xpCurveConfig.GetBossXpFraction(_bossKillCount);

            _bossKillCount++;
            GrantXp(xpRequiredForNextLevel * bossXpFraction);
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

        /// <summary>
        /// Called by the choice UI once the player picks a stat card. Applies
        /// the stat and unfreezes the game after a short delay so the player
        /// has a beat to re-orient before gameplay resumes.
        /// </summary>
        public void ResolveStatChoice(StatModifier chosenModifier)
        {
            statSheet.ApplyModifier(chosenModifier);
            EventBus.Emit(new StatChoiceResolvedEvent { ChosenModifier = chosenModifier });

            Invoke(nameof(Unfreeze), UnfreezeDelaySeconds);
        }

        /// <summary>Called by the choice UI once the player picks an ability card. Mirrors ResolveStatChoice.</summary>
        public void ResolveAbilityChoice(AbilityChoiceOption chosenOption)
        {
            chosenOption.Entry.Grant(chosenOption.Rarity);
            EventBus.Emit(new AbilityChoiceResolvedEvent { ChosenOption = chosenOption });

            Invoke(nameof(Unfreeze), UnfreezeDelaySeconds);
        }

        private void Unfreeze()
        {
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
