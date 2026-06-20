using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Enum used as a key everywhere: EnemyPool dictionary, WaveConfig spawn groups, SpatialGrid.
    /// Add new types here and create a matching EnemyTypeSO asset + prefab.
    /// </summary>
    public enum EnemyType
    {
        Zombie = 0,
        Spider = 1,
        SpiderMinion = 2,
        EyeWinged = 3,
        TornadoGhost = 4,
        Boss = 10,
    }

    /// <summary>
    /// Base stats for one enemy type. DifficultyScaler multiplies these at runtime.
    /// Never hardcode stats on prefabs — always read from this SO.
    ///
    /// Create via: right-click Project → Create → Game/EnemyType
    /// </summary>
    [CreateAssetMenu(menuName = "Game/EnemyType", fileName = "ET_Zombie")]
    public class EnemyTypeSo : ScriptableObject
    {
        [Header("Identity")]
        public EnemyType type;
        public GameObject prefab;

        [Header("Pool")]
        [Tooltip("How many instances to pre-allocate. Set to your expected max on screen + buffer.")]
        public int poolSize = 100;

        [Header("Base stats — DifficultyScaler multiplies these")]
        public float baseHp = 10f;
        public float baseSpeed = 3f;
        public float baseDamage = 1f;

        [Header("Behaviour")]
        [Tooltip("How close before the enemy tries to attack")]
        public float attackRange = 0.8f;
        [Tooltip("Seconds between attacks")]
        public float attackCooldown = 1.0f;
        [Tooltip("How strongly this enemy avoids overlapping with neighbours (0 = no separation)")]
        [Range(0f, 5f)]
        public float separationForce = 1.5f;

        [Header("Rewards")]
        [Tooltip("Base XP dropped on death, before time-based and miniboss scaling (see XpCurveConfigSo).")]
        public float xpValue = 1f;

        [Header("Miniboss")]
        [Tooltip("If true, this type is eligible to spawn in a miniboss state " +
                 "(1.5x stats, 1.5x XP, glow VFX). Actual miniboss roll happens at spawn time.")]
        public bool canBeMiniboss = true;
    }
}
