using Difficulty;
using Enemies;
using UnityEngine;
using Waves;

namespace Core
{
    /// <summary>
    /// Bootstraps the game. Holds serialized references to all top-level systems
    /// and calls StartWave(0) to kick things off.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private DifficultyScaler difficultyScaler;

        [Header("Settings")]
        [SerializeField] private float delayBeforeFirstWave = 2f;

        private bool _gameOver;

        private void Awake()
        {
            // Validate all required references up front — fail loud, fail early
            Debug.Assert(waveManager, "GameManager: WaveManager ref missing");
            Debug.Assert(enemyPool, "GameManager: EnemyPool ref missing");
            Debug.Assert(difficultyScaler, "GameManager: DifficultyScaler ref missing");
        }

        private void Start()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            Invoke(nameof(StartGame), delayBeforeFirstWave);
        }

        private void OnDestroy()
        {
            EventBus.ClearAll();
        }

        private void StartGame()
        {
            waveManager.StartWave(0);
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            if (_gameOver) return;
            _gameOver = true;
            Debug.Log("Game Over");
            // TODO: show game-over screen, stop WaveManager
        }
    }
}
