using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Spider ability: periodically summons SpiderMinion copies around itself.
    /// The Spider itself does NOT attack the player directly — it stays at range
    /// and lets its minions do the damage. It has no IEnemyAttackBehaviour,
    /// so BehaviourController will never enter attack state for it; it just
    /// keeps moving to maintain its preferred kiting range and summons minions
    /// on a timer via IPeriodicAbility.
    ///
    /// Minions per summon is 2, matching the design doc.
    ///
    /// Attach to: the Spider prefab alongside BehaviourController.
    /// Remove MeleeAttackBehaviour from the Spider prefab if present.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    [RequireComponent(typeof(EnemyView))]
    public class SpiderAttackBehaviour : MonoBehaviour, IPeriodicAbility
    {
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
                Debug.LogError("SpiderAttackBehaviour: EnemyView.Pool not set.", this);
                return;
            }

            for (var i = 0; i < minionsPerSummon; i++)
            {
                Vector3 RollOffsetPosition()
                {
                    var offset = Random.insideUnitCircle * summonRadius;
                    return spiderPosition + new Vector3(offset.x, 0f, offset.y);
                }

                _view.Pool.Get(EnemyType.SpiderMinion, RollOffsetPosition(), RollOffsetPosition);
            }
        }
    }
}
