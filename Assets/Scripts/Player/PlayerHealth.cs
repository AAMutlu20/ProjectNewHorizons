using Core;
using Enemies;
using Stats;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages player HP. Reads Vitality, Health Regeneration, and Damage
    /// Reduction from StatSheet rather than holding its own copies. Receives
    /// damage via the EventBus (EnemyAttackEvent) and emits events that
    /// drive the HUD — no direct HUD references here.
    ///
    /// Attach to: PlayerRoot alongside PlayerController and StatSheet.
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    public class PlayerHealth : MonoBehaviour
    {
        // How close an attack's reported position must be to count as a hit on
        // this player. Wider than melee range so ranged enemy attacks (which
        // report their impact point, not the enemy's position) still connect.
        private const float AttackHitRadius = 1.5f;
        private const float PercentToFraction = 100f;

        [SerializeField] private float baseMaxHp = 200f;

        [Header("Invincibility frames after taking a hit")]
        [SerializeField] private float iFrameDuration = 0.5f;

        private StatSheet _statSheet;
        private float _currentHp;
        private float _iFrameTimer;
        private bool _isDead;

        public bool IsAlive => _currentHp > 0f;
        public float MaxHp => baseMaxHp + _statSheet.GetTotal(StatType.Vitality);

        private void Awake()
        {
            _statSheet = GetComponent<StatSheet>();
        }

        private void Start()
        {
            _currentHp = MaxHp;
            EventBus.Subscribe<EnemyAttackEvent>(OnEnemyAttack);
            EmitHealthChanged();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyAttackEvent>(OnEnemyAttack);
        }

        private void Update()
        {
            TickInvincibilityFrames();
            RegenerateHealth();
        }

        private void TickInvincibilityFrames()
        {
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        private void RegenerateHealth()
        {
            if (_isDead || _currentHp >= MaxHp) return;

            var regenPercentPerSecond = _statSheet.GetTotal(StatType.HealthRegeneration);
            if (regenPercentPerSecond <= 0f) return;

            var healPerSecond = MaxHp * (regenPercentPerSecond / PercentToFraction);
            Heal(healPerSecond * Time.deltaTime);
        }

        private void OnEnemyAttack(EnemyAttackEvent attack)
        {
            if (_isDead) return;
            if (_iFrameTimer > 0f) return; // still invincible

            var distanceToAttack = Vector3.Distance(transform.position, attack.Position);
            if (distanceToAttack > AttackHitRadius) return;

            TakeDamage(attack.Damage);
        }

        private void TakeDamage(float amount)
        {
            if (_isDead) return;

            var damageReductionPercent = _statSheet.GetTotal(StatType.DamageReduction);
            var mitigatedAmount = amount * (1f - damageReductionPercent / PercentToFraction);

            _currentHp = Mathf.Max(0f, _currentHp - mitigatedAmount);
            _iFrameTimer = iFrameDuration;

            EmitHealthChanged();

            if (_currentHp <= 0f)
                Die();
        }

        private void Heal(float amount)
        {
            _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
            EmitHealthChanged();
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            EventBus.Emit(new PlayerDiedEvent());
        }

        private void EmitHealthChanged()
        {
            EventBus.Emit(new PlayerHealthChangedEvent
            {
                Current = _currentHp,
                Max = MaxHp,
            });
        }
    }
}
