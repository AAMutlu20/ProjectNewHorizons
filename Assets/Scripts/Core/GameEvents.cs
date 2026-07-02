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
        public bool IsBoss;
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
    // Melee VFX events — emitted by MeleeWeapon and melee enchants so
    // VFX scripts can react without being coupled to gameplay logic.
    // -------------------------------------------------------------------------

    /// <summary>Emitted by MeleeWeapon every time a hit lands on an enemy.</summary>
    public struct MeleeHitEvent
    {
        public UnityEngine.Vector3 HitPosition; // world position of the struck enemy
        public float DamageDealt;
        public bool IsCrit;
    }

    /// <summary>Emitted by CleavingAttacksEnchant when a trigger swing cleaves.</summary>
    public struct CleavingTriggeredEvent
    {
        public UnityEngine.Vector3 SwingOrigin; // player's position at the time of the cleave
    }

    /// <summary>Emitted by LifestealEnchant each time a hit heals the player.</summary>
    public struct LifestealHealEvent
    {
        public float AmountHealed;
        public UnityEngine.Vector3 HitPosition;
    }
}
