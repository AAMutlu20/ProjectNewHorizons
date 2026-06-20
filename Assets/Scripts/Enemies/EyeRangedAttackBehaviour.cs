using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Eye attack: fires a projectile at the player on cooldown instead of
    /// attacking by contact. attackRange on the EnemyTypeSo should be set
    /// large (effectively the Eye's firing range) since this never melees.
    ///
    /// Attach to: the EyeWinged prefab, alongside BehaviourController,
    /// instead of MeleeAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class EyeRangedAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifetime = 5f;

        private BehaviourController _behaviour;

        private void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
            Debug.Assert(projectilePool, "EyeRangedAttackBehaviour: projectilePool not assigned.", this);
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            enemy.State = EnemyState.Attacking;
            enemy.AttackTimer -= deltaTime;
            enemy.Velocity = Vector3.zero;

            if (!enemy.CanAttack) return;

            enemy.AttackTimer = enemy.TypeSo.attackCooldown;
            FireAtPlayer(enemy);
        }

        private void FireAtPlayer(EnemyData enemy)
        {
            if (!_behaviour.PlayerTransform) return;

            var direction = (_behaviour.PlayerTransform.position - enemy.Position).normalized;
            projectilePool.Fire(enemy.Position, direction, projectileSpeed, enemy.Damage, projectileLifetime);
        }
    }
}
