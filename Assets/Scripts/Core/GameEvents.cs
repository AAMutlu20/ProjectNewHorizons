using Enemies;
using Stats;

namespace Core
{
    /// <summary>
    /// All event structs used by the wave/enemy system.
    /// Plain data structs — no logic, no MonoBehaviour.
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
}