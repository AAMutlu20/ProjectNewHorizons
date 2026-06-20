using System;
using System.Linq;
using Enemies;
using UnityEngine;

namespace Waves
{
    /// <summary>
    /// One group of enemies within a wave.
    /// SpawnScheduler reads these and fires enemies at the right time.
    /// </summary>
    [Serializable]
    public class SpawnGroup
    {
        [Tooltip("Which enemy type to spawn")]
        public EnemyType enemyType = EnemyType.Zombie;

        [Tooltip("Total enemies in this group")]
        public int count = 10;

        [Tooltip("Seconds into the wave before this group starts spawning")]
        public float startTime;

        [Tooltip("Seconds between consecutive spawns in this group")]
        public float interval = 0.5f;

        [Tooltip("Where enemies appear")]
        public SpawnShapeType spawnShape = SpawnShapeType.Ring;

        [Tooltip("For Arc / DirectedArc: total angle of the spawn arc in degrees")]
        [Range(10f, 360f)]
        public float arcAngle = 90f;

        [Tooltip("For Arc: direction the arc faces in degrees (0 = right, 90 = up)")]
        public float arcDirection;
    }

    /// <summary>
    /// ScriptableObject defining all spawn groups for one wave.
    /// Create in Project: right-click → Create → Game/WaveConfig
    ///
    /// Tip: name your assets Wave_01_Intro, Wave_05_Rush, Wave_10_Boss etc.
    /// WaveManager loads them by index — naming is for human readability only.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/WaveConfig", fileName = "Wave_01")]
    public class WaveConfig : ScriptableObject
    {
        [Tooltip("How long this wave lasts before triggering WaveComplete regardless of enemy count. " +
                 "Set to 0 to disable time limit (wave ends only when all enemies die).")]
        public float waveDuration = 60f;

        [Tooltip("Minimum seconds between the end of this wave and the start of the next")]
        public float restDuration = 3f;

        [Tooltip("Spawn groups — can overlap in time. Order doesn't matter; startTime drives sequencing.")]
        public SpawnGroup[] groups = {
            new() { count = 10, startTime = 0f, interval = 0.5f }
        };

        /// <summary>Total enemies across all groups (before difficulty multiplier).</summary>
        public int TotalBaseCount()
        {
            return groups.Sum(g => g.count);
        }
    }
}
