using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Boss ability: every checkInterval seconds, counts currently-alive
    /// EyeWinged escorts and spawns enough to refill the cap, per the design
    /// doc's "every 10 seconds if eyes &lt; 2 then spawn the needed to fill
    /// the cap of 2" rule. Spawned escorts are flagged as minibosses.
    ///
    /// Attach to: the Boss prefab, alongside BehaviourController.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    [RequireComponent(typeof(EnemyView))]
    public class BossEscortBehaviour : MonoBehaviour, IPeriodicAbility
    {
        [SerializeField] private int escortCap = 2;
        [SerializeField] private float checkInterval = 10f;
        [SerializeField] private float spawnRadius = 6f;

        private BehaviourController _behaviour;
        private EnemyView _view;
        private float _checkTimer;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
            _view = GetComponent<EnemyView>();
            _checkTimer = checkInterval;
        }

        public void TickAbility(ref EnemyData enemy, float deltaTime)
        {
            _checkTimer -= deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = checkInterval;
            RefillEscortsIfNeeded(enemy.Position);
        }

        private void RefillEscortsIfNeeded(Vector3 bossPosition)
        {
            var aliveEscorts = CountAliveEscorts();
            var escortsNeeded = escortCap - aliveEscorts;

            for (var i = 0; i < escortsNeeded; i++)
                SpawnEscort(bossPosition);
        }

        private int CountAliveEscorts()
        {
            if (_behaviour.ActiveEnemies == null) return 0;

            var count = 0;
            foreach (var activeEnemy in _behaviour.ActiveEnemies)
            {
                if (activeEnemy.Type == EnemyType.EyeWinged && activeEnemy.Data.IsAlive)
                    count++;
            }
            return count;
        }

        private void SpawnEscort(Vector3 bossPosition)
        {
            if (!_view.Pool)
            {
                Debug.LogError("BossEscortBehaviour: EnemyView.Pool is not set, cannot spawn escorts.", this);
                return;
            }

            var offset = Random.insideUnitCircle * spawnRadius;
            var spawnPosition = bossPosition + new Vector3(offset.x, 0f, offset.y);
            _view.Pool.Get(EnemyType.EyeWinged, spawnPosition, isMiniboss: true);
        }
    }
}
