using Core;
using Player;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Tornado Ghost attack: while the player is within attackRange, pulls
    /// them toward the ghost continuously and deals damage on the normal
    /// attack cooldown — a combined AOE pull-and-damage effect, not two
    /// separate abilities.
    ///
    /// Attach to: the TornadoGhost prefab, alongside BehaviourController,
    /// instead of MeleeAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class TornadoGhostAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        [Header("Pull")]
        [SerializeField] private float pullStrength = 8f;

        private BehaviourController _behaviour;
        private PlayerController _cachedPlayerController;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.AttackTimer -= deltaTime;
            enemy.Velocity = Vector3.zero;

            PullPlayerTowardSelf(enemy);

            if (!enemy.CanAttack) return;

            enemy.AttackTimer = enemy.TypeSo.attackCooldown;
            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = enemy.Damage,
                Position = enemy.Position,
            });
        }

        private void PullPlayerTowardSelf(EnemyData enemy)
        {
            if (!_behaviour.PlayerTransform) return;

            if (!_cachedPlayerController)
                _cachedPlayerController = _behaviour.PlayerTransform.GetComponent<PlayerController>();

            if (!_cachedPlayerController) return;

            var pullDirection = (enemy.Position - _behaviour.PlayerTransform.position).normalized;
            _cachedPlayerController.ApplyExternalPull(pullDirection * pullStrength);
        }
    }
}
