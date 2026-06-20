using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Default melee-range attack: stand still and hit the player on cooldown.
    /// This is the original BehaviourController attack logic, extracted so
    /// archetypes with different attack patterns (ranged, AOE, summon) can
    /// swap it out by attaching a different IEnemyAttackBehaviour instead.
    ///
    /// Attach to: prefabs that should attack by simple contact (e.g. Zombie).
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class MeleeAttackBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
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
            });
        }
    }
}
