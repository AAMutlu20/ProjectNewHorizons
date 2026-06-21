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
    /// drive the HUD — no direct HUD references here. Attacks that carry a
    /// slow (e.g. Spider webs) are forwarded to SlowEffectController.
    ///
    /// If DarkShieldController is granted and has layers available, it blocks
    /// the hit entirely before damage or i-frames are even considered —
    /// per the design doc, the shield "blocks any damage they will receive."
    ///
    /// Heal and IncreaseMaxHpPermanently are public so melee enchants (e.g.
    /// Lifesteal) and other systems can call them directly without needing
    /// their own parallel healing implementation.
    ///
    /// Attach to: PlayerRoot alongside PlayerController, StatSheet, and
    /// DarkShieldController (DarkShieldController is optional — not every
    /// build of the player needs it granted).
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [RequireComponent(typeof(SlowEffectController))]
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
        private SlowEffectController _slowEffects;
        private DarkShieldController _shield; // optional — null if not granted on this player

        private float _currentHp;
        private float _iFrameTimer;
        private bool _isDead;

        public bool IsAlive => _currentHp > 0f;
        public float MaxHp => baseMaxHp + _statSheet.GetTotal(StatType.Vitality);

        private void Awake()
        {
            _statSheet = GetComponent<StatSheet>();
            _slowEffects = GetComponent<SlowEffectController>();
            _shield = GetComponent<DarkShieldController>(); // may be null — that's fine
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

            var distanceToAttack = Vector3.Distance(transform.position, attack.Position);
            if (distanceToAttack > AttackHitRadius) return;

            ApplySlowIfAny(attack);

            if (_iFrameTimer > 0f) return; // still invincible — damage blocked, slow still applies

            // Shield blocks the hit entirely, consuming a layer, only for
            // hits that would otherwise actually land — no point spending a
            // layer on a hit i-frames would have nullified for free.
            if (_shield != null && _shield.TryBlockDamage()) return;

            TakeDamage(attack.Damage);
        }

        private void ApplySlowIfAny(EnemyAttackEvent attack)
        {
            if (attack.SlowFraction <= 0f || attack.SlowDuration <= 0f) return;
            _slowEffects.ApplySlow(attack.SlowFraction, attack.SlowDuration);
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

        /// <summary>Heals the player by amount, clamped to MaxHp. Public so abilities/enchants (e.g. Lifesteal) can call it directly.</summary>
        public void Heal(float amount)
        {
            _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
            EmitHealthChanged();
        }

        /// <summary>
        /// Permanently raises baseMaxHp (e.g. Lifesteal's Legendary tier).
        /// Distinct from Heal — this raises the ceiling itself, not just
        /// current HP toward an existing ceiling.
        /// </summary>
        public void IncreaseMaxHpPermanently(float amount)
        {
            baseMaxHp += amount;
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
