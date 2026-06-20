using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Spider attack and ability: web-slows the player on contact (gated by
    /// attackRange, like MeleeAttackBehaviour), and separately summons
    /// smaller, faster SpiderMinion copies on its own timer regardless of
    /// distance to the player (IPeriodicAbility — runs through the existing
    /// managed update loop, no per-enemy Update()).
    ///
    /// Only the base Spider summons — SpiderMinion uses MeleeAttackBehaviour
    /// instead, so minions don't recursively spawn more minions.
    ///
    /// Attach to: the Spider prefab, alongside BehaviourController, instead
    /// of MeleeAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    [RequireComponent(typeof(EnemyView))]
    public class SpiderAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour, IPeriodicAbility
    {
        [Header("Web slow")]
        [SerializeField] private float webSlowFraction = 0.4f;
        [SerializeField] private float webSlowDuration = 2f;

        [Header("Minion summon")]
        [SerializeField] private int minionsPerSummon = 2;
        [SerializeField] private float summonInterval = 8f;
        [SerializeField] private float summonRadius = 2f;

        private EnemyView _view;
        private float _summonTimer;

        private void Awake()
        {
            _view = GetComponent<EnemyView>();
            _summonTimer = summonInterval;
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.AttackTimer -= deltaTime;
            enemy.Velocity = Vector3.zero;

            if (!enemy.CanAttack) return;

            enemy.AttackTimer = enemy.TypeSo.attackCooldown;
            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = enemy.Damage,
                Position = enemy.Position,
                SlowFraction = webSlowFraction,
                SlowDuration = webSlowDuration,
            });
        }

        public void TickAbility(ref EnemyData enemy, float deltaTime)
        {
            _summonTimer -= deltaTime;
            if (_summonTimer > 0f) return;

            _summonTimer = summonInterval;
            SummonMinions(enemy.Position);
        }

        private void SummonMinions(Vector3 spiderPosition)
        {
            if (!_view.Pool)
            {
                Debug.LogError("SpiderAttackBehaviour: EnemyView.Pool is not set, cannot summon minions.", this);
                return;
            }

            for (var i = 0; i < minionsPerSummon; i++)
            {
                var offset = Random.insideUnitCircle * summonRadius;
                var spawnPosition = spiderPosition + new Vector3(offset.x, 0f, offset.y);
                _view.Pool.Get(EnemyType.SpiderMinion, spawnPosition);
            }
        }
    }
}
