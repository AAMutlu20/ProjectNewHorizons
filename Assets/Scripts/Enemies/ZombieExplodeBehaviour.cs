using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Zombie attack: on reaching the player, explodes once — a single direct
    /// hit (not an AOE), with a VFX flag for the explosion visual — then the
    /// zombie dies. No repeating attack cooldown, unlike MeleeAttackBehaviour.
    ///
    /// Attach to: the Zombie prefab, alongside BehaviourController, instead
    /// of MeleeAttackBehaviour.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    [RequireComponent(typeof(EnemyView))]
    public class ZombieExplodeBehaviour : MonoBehaviour, IEnemyAttackBehaviour
    {
        private EnemyView _view;
        private bool _hasExploded;

        private void Awake()
        {
            _view = GetComponent<EnemyView>();
        }

        public void TickAttack(ref EnemyData enemy, float deltaTime)
        {
            if (_hasExploded) return;

            enemy.State = EnemyState.Attacking;
            enemy.Velocity = Vector3.zero;
            _hasExploded = true;

            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = enemy.Damage,
                Position = enemy.Position,
            });

            EventBus.Emit(new ZombieExplodedEvent
            {
                Position = enemy.Position,
            });

            _view.TakeDamage(enemy.Hp);
        }
    }

    /// <summary>Emitted when a zombie explodes, for VFX/audio systems to react to.</summary>
    public struct ZombieExplodedEvent
    {
        public Vector3 Position;
    }
}
