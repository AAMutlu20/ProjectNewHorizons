using UnityEngine;

namespace Difficulty
{
    /// <summary>
    /// Pure data struct passed to SpawnScheduler and EnemyPool each wave.
    /// No MonoBehaviour — lives on the stack, zero allocation.
    /// </summary>
    public struct DifficultyParams
    {
        public float CountMult;    // multiplier on SpawnGroup.count
        public float SpeedMult;    // multiplier on EnemyTypeSO.baseSpeed
        public float HpMult;       // multiplier on EnemyTypeSO.baseHp
        public float DamageMult;   // multiplier on EnemyTypeSO.baseDamage
        public float SpawnRadius;  // world-units from arena centre where enemies appear
        public float Budget;       // max enemies active simultaneously (soft cap)
    }

    /// <summary>
    /// ScriptableObject that drives DifficultyScaler.
    /// Designer edits AnimationCurves in the Inspector — no code changes for balance passes.
    ///
    /// Create via: right-click Project → Create → Game/DifficultyConfig
    /// </summary>
    [CreateAssetMenu(menuName = "Game/DifficultyConfig", fileName = "DifficultyConfig_Default")]
    public class DifficultyConfig : ScriptableObject
    {
        [Header("Wave count")]
        public int totalWaves = 20;

        [Header("Curves - X axis is 0..1 (normalised wave index), Y is the multiplier")]
        [Tooltip("How many more enemies spawn as waves progress")]
        public AnimationCurve spawnCountMultiplier = AnimationCurve.Linear(0, 1, 1, 3);

        [Tooltip("How much faster enemies move")]
        public AnimationCurve enemySpeedMultiplier = AnimationCurve.Linear(0, 1, 1, 2);

        [Tooltip("How much more HP enemies have")]
        public AnimationCurve enemyHpMultiplier = AnimationCurve.Linear(0, 1, 1, 4);

        [Tooltip("How much more damage enemies deal")]
        public AnimationCurve enemyDamageMultiplier = AnimationCurve.Linear(0, 1, 1, 2);

        [Tooltip("Simultaneous enemy budget - ramps up then plateaus")]
        public AnimationCurve activeBudget = AnimationCurve.EaseInOut(0, 20, 1, 150);

        [Header("Spawn radius - how far from centre enemies appear")]
        public float minSpawnRadius = 12f;
        public float maxSpawnRadius = 18f;
    }
}