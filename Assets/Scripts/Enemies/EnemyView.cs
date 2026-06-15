using Core;
using Difficulty;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// The only MonoBehaviour on an enemy prefab.
    /// Owns the EnemyData struct and syncs it to the Transform and Animator.
    /// Receives damage via TakeDamage() — called by player attack scripts.
    ///
    /// Attach to: root of every enemy prefab.
    /// </summary>
    [RequireComponent(typeof(BehaviourController))]
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] private Animator animator; // graybox enemies have none

        // The actual runtime data — ref-accessible so BehaviourController writes directly
        private EnemyData _data;

        // Public read access for SpatialGrid and other systems
        public EnemyData Data => _data;
        public EnemyType Type => _data.Type;

        // Ref access for BehaviourController — avoids copy-on-read overhead
        public ref EnemyData DataRef => ref _data;

        // Set by EnemyPool after Get() — the pool this view belongs to
        [System.NonSerialized] public EnemyPool Pool;

        private BehaviourController _behaviour;
        private EnemyState _lastState;

        // Animator parameter hashes — cache them, string lookup is slow
        private static readonly int AnimState = Animator.StringToHash("State");
        private static readonly int AnimDead = Animator.StringToHash("Dead");

        void Awake()
        {
            _behaviour = GetComponent<BehaviourController>();
        }

        // Called by EnemyPool

        /// <summary>Initialise this view with fresh data. Called immediately after Get() from pool.</summary>
        public void Init(EnemyTypeSo typeSo, DifficultyParams diff, Vector2 worldPos,
            Transform playerTransform, SpatialGrid grid, System.Collections.Generic.List<EnemyView> activeList)
        {
            _data = EnemyData.Create(typeSo, diff, worldPos);
            transform.position = worldPos;

            // Wire BehaviourController references
            _behaviour.PlayerTransform = playerTransform;
            _behaviour.Grid = grid;
            _behaviour.ActiveEnemies = activeList;

            if (animator != null) animator.SetInteger(AnimState, (int)EnemyState.Spawning);
            _lastState = EnemyState.Spawning;
        }

        /// <summary>
        /// Called by EnemyPool once per frame instead of using Unity's Update().
        /// Keeps the number of active MonoBehaviour Update callbacks to a minimum.
        /// </summary>
        public void ManagedUpdate(float dt)
        {
            if (_data.State == EnemyState.Inactive) return;

            // Tick the behaviour (writes to _data)
            _behaviour.Tick(dt);

            // Sync transform from data position
            transform.position = new Vector3(_data.Position.x, _data.Position.y, 0f);

            // Flip sprite based on movement direction
            if (_data.Velocity.x != 0f)
                transform.localScale = new Vector3(
                    _data.Velocity.x < 0 ? -1f : 1f, 1f, 1f);

            // Drive Animator only when state changes — avoids SetInteger every frame
            if (animator && _data.State != _lastState)
            {
                animator.SetInteger(AnimState, (int)_data.State);
                if (_data.State == EnemyState.Dying)
                    animator.SetTrigger(AnimDead);
                _lastState = _data.State;
            }

            // Return to pool after death animation finishes
            // For graybox: return immediately on death. With animator: check normalizedTime.
            if (_data.State != EnemyState.Dying) return;
            var animDone = !animator ||
                           animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
            if (animDone)
                Pool.Return(this);
        }

        // Damage

        /// <summary>Called by player attack / projectile scripts on collision.</summary>
        public void TakeDamage(float amount)
        {
            if (!_data.IsAlive) return;

            _data.Hp -= amount;
            if (!(_data.Hp <= 0f)) return;
            _data.Hp = 0f;
            _behaviour.OnDeath();
            EventBus.Emit(new EnemyDiedEvent { Type = _data.Type, Position = _data.Position });
        }
    }
}
