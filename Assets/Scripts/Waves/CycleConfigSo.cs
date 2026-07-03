using UnityEngine;

namespace Waves
{
    /// <summary>
    /// Defines one full enemy-introduction cycle: an ordered list of phases,
    /// each introducing a new enemy type and ramping its count, ending in a
    /// boss phase. WaveDirector loops this repeatedly, escalating difficulty
    /// via CycleEscalation after every boss kill.
    ///
    /// Create via: right-click Project → Create → Game/Waves/CycleConfig
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Waves/CycleConfig", fileName = "CycleConfig_Default")]
    public class CycleConfigSo : ScriptableObject
    {
        [Tooltip("Ordered phases — Zombie, then Spider, then Eye, then TornadoGhost, then Boss, " +
                 "matching the design doc's introduction order.")]
        public EnemyCyclePhase[] phases =
        {
            new() { enemyType = Enemies.EnemyType.Zombie, startingCount = 5, spawnInterval = 1.2f, baseDuration = 60f },
            new() { enemyType = Enemies.EnemyType.Spider, startingCount = 3, spawnInterval = 2f, baseDuration = 60f },
            new() { enemyType = Enemies.EnemyType.EyeWinged, startingCount = 9, spawnInterval = 2.5f, baseDuration = 60f },
            new() { enemyType = Enemies.EnemyType.TornadoGhost, startingCount = 5, spawnInterval = 3f, baseDuration = 60f },
        };

        [Header("Miniboss")]
        [Tooltip("Base chance (0..1) that an eligible spawn becomes a miniboss before CycleEscalation bonuses apply.")]
        [Range(0f, 1f)]
        public float baseMinibossChance = 0.05f;

        [Header("Cycle restart")]
        [Tooltip("Seconds to wait after the player picks their Legendary reward before " +
                 "the next cycle begins. Gives the player a beat to re-orient.")]
        public float delayAfterCycleEnd = 0.5f;
    }
}
