using Difficulty;
using Enemies;
using UnityEngine;
using Waves;

namespace Core
{
    /// <summary>
    /// Bootstraps the game. Holds serialized references to top-level systems
    /// for validation and watches for the player's death.
    ///
    /// WaveDirector starts itself on its own Start() — it owns the infinite
    /// cycle's lifecycle, so GameManager no longer needs to kick it off.
    ///
    /// Attach to: a GameObject in [Systems].
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private DifficultyScaler difficultyScaler;

        private bool _gameOver;

        private void Awake()
        {
            // Validate all required references up front — fail loud, fail early
            Debug.Assert(waveDirector, "GameManager: WaveDirector ref missing");
            Debug.Assert(enemyPool, "GameManager: EnemyPool ref missing");
            Debug.Assert(difficultyScaler, "GameManager: DifficultyScaler ref missing");
        }

        private void Start()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnDestroy()
        {
            EventBus.ClearAll();
        }

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            if (_gameOver) return;
            _gameOver = true;
            Debug.Log("Game Over");
            // TODO: show game-over screen, stop WaveDirector
        }
    }
}
