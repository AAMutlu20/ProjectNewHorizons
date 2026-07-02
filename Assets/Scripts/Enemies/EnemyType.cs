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

        [Tooltip("LOCAL foot-to-pivot offset for this archetype -- e.g. half the capsule's " +
                 "actual scaled height, if the capsule's pivot is its center. Applied by " +
                 "SpawnPositionResolver ON TOP OF THE REAL DETECTED GROUND HEIGHT at spawn " +
                 "time (via a downward raycast), NOT as an absolute world Y -- this correctly " +
                 "handles raised platforms, ramps, or uneven floor noise identically to flat ground.")]
        public float groundOffsetY = 1f;

        [Header("Pool")]
        [Tooltip("How many instances to pre-allocate. Set to your expected max on screen + buffer.")]
        public int poolSize = 100;

        [Tooltip("Weighted budget cost of one instance of this type. The active enemy cap (DifficultyParams.Budget) " +
                 "is expressed in these units — cheap chaff costs 1, tanky elites cost more. " +
                 "Design doc Section 1.4: this decouples visual density from difficulty composition.")]
        public int budgetCost = 1;

        [Header("Base stats — DifficultyScaler multiplies these")]
        public float baseHp = 10f;
        public float baseSpeed = 3f;
        public float baseDamage = 1f;

        [Header("Behaviour")]
        [Tooltip("How close before the enemy tries to attack")]
        public float attackRange = 0.8f;
        [Tooltip("Seconds between attacks")]
        public float attackCooldown = 1.0f;
        [Tooltip("How strongly this enemy avoids overlapping with neighbours (0 = no separation). " +
                 "This is the SOLE separation scaling value — BehaviourController no longer applies " +
                 "an additional hardcoded multiplier on top, so this field is the direct tuning lever.")]
        [Range(0f, 5f)]
        public float separationForce = 1.5f;

        [Tooltip("Preferred stand-off distance from the player in world units. " +
                 "0 = pure melee (close to zero). Ranged enemies (Eye, Spider) should set this " +
                 "to the distance they want to maintain while shooting/casting. " +
                 "BehaviourController will seek toward the player until this distance, " +
                 "then kite backward if closer, instead of driving all the way to attackRange.")]
        public float preferredRange = 0f;

        [Tooltip("How aggressively the enemy kites backward when inside preferredRange. " +
                 "0 = no kiting (pure melee). 1 = full reverse seek. " +
                 "Only relevant when preferredRange > 0.")]
        [Range(0f, 1f)]
        public float kiteStrength = 0f;

        [Header("Rewards")]
        [Tooltip("Base XP dropped on death, before time-based and miniboss scaling (see XpCurveConfigSo).")]
        public float xpValue = 1f;

        [Header("Miniboss")]
        [Tooltip("If true, this type is eligible to spawn in a miniboss state " +
                 "(1.5x stats, 1.5x XP, glow VFX). Actual miniboss roll happens at spawn time.")]
        public bool canBeMiniboss = true;
    }
}
