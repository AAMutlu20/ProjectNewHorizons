using Abilities;
using Enemies;
using Stats;

namespace Core
{
    /// <summary>
    /// All event structs used by the wave/enemy system.
    /// Plain data structs -- no logic, no MonoBehaviour.
    /// </summary>

    public struct WaveStartedEvent
    {
        public int Wave;
        public float WaveStartTime;
    }

    public struct WaveCompleteEvent
    {
        public int  Wave;
        public float Duration; // seconds the wave took
    }

    public struct EnemyDiedEvent
    {
        public EnemyType Type;
        public UnityEngine.Vector3 Position;
        public float XpValue;
        public bool IsMiniboss;
    }

    public struct EnemyReturnedEvent
    {
        // Fired by EnemyPool.Return() - tells WaveManager the active count dropped
    }

    public struct PlayerDiedEvent { }

    public struct PlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }

    public struct DifficultyChangedEvent
    {
        public int   Wave;
        public float CountMult;
        public float SpeedMult;
    }

    public struct StatCollectedEvent
    {
        public StatType StatType;
        public Rarity Rarity;
        public float NewTotal;
    }

    public struct ExperienceChangedEvent
    {
        public float CurrentXp;
        public float XpToNextLevel;
        public int Level;
    }

    public struct LevelUpEvent
    {
        public int NewLevel;
        public bool IsAbilityLevel;
    }

    public struct StatChoicePresentedEvent
    {
        public System.Collections.Generic.List<StatChoiceOption> Choices;
    }

    public struct StatChoiceResolvedEvent
    {
        public StatModifier ChosenModifier;
    }

    public struct AbilityChoicePresentedEvent
    {
        public System.Collections.Generic.List<AbilityChoiceOption> Choices;
    }

    public struct AbilityChoiceResolvedEvent
    {
        public AbilityChoiceOption ChosenOption;
    }

    // -------------------------------------------------------------------------
    // Boss phase events — emitted by WaveDirector so audio/UI can react to the
    // boss fight starting and ending without coupling to WaveDirector directly.
    // -------------------------------------------------------------------------


    // -------------------------------------------------------------------------
    // Melee VFX / Audio events
    // -------------------------------------------------------------------------

    public struct MeleeHitEvent
    {
        public UnityEngine.Vector3 HitPosition;
        public float DamageDealt;
        public bool IsCrit;
    }

    public struct CleavingTriggeredEvent
    {
        public UnityEngine.Vector3 SwingOrigin;
    }

    public struct LifestealHealEvent
    {
        public float AmountHealed;
        public UnityEngine.Vector3 HitPosition;
    }

    /// <summary>Fired by WaveDirector at the 4-minute mark. LevelSystem listens
    /// and presents the Legendary reward choice before the cycle restarts.</summary>
    public struct CycleEndedEvent
    {
        public int CycleNumber;
    }

    /// <summary>Fired by LevelSystem after the player picks their Legendary reward.
    /// WaveDirector listens to restart the cycle.</summary>
    public struct CycleRewardResolvedEvent { }
}
