using Core;
using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages player HP. Receives damage via the EventBus (EnemyAttackEvent)
    /// and emits events that drive the HUD — no direct HUD references here.
    ///
    /// Attach to: PlayerRoot alongside PlayerController.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;

        [Header("Invincibility frames after taking a hit")]
        [SerializeField] private float iFrameDuration = 0.5f;

        private float CurrentHp { get; set; }
        public bool IsAlive => CurrentHp > 0f;

        private float _iFrameTimer;
        private bool _dead;

        private void Start()
        {
            CurrentHp = maxHp;
            EventBus.Subscribe<EnemyAttackEvent>(OnEnemyAttack);
            EmitHealthChanged();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyAttackEvent>(OnEnemyAttack);
        }

        private void Update()
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        // Damage

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            if (_dead) return;
            if (_iFrameTimer > 0f) return; // still invincible

            // Only take damage if the attack is near enough (simple distance check)
            var dist = Vector3.Distance(transform.position, evt.Position);
            if (dist > 1.5f) return;

            TakeDamage(evt.Damage);
        }

        private void TakeDamage(float amount)
        {
            if (_dead) return;

            CurrentHp   = Mathf.Max(0f, CurrentHp - amount);
            _iFrameTimer = iFrameDuration;

            EmitHealthChanged();

            if (CurrentHp <= 0f)
                Die();
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;
            Debug.Log("Player died");
            EventBus.Emit(new PlayerDiedEvent());
        }

        private void EmitHealthChanged()
        {
            EventBus.Emit(new PlayerHealthChangedEvent
            {
                Current = CurrentHp,
                Max = maxHp,
            });
        }
    }
}
