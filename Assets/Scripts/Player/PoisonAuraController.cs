using Core;
using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Poison Aura: while granted, continuously damages every alive enemy
    /// within Range of the player on a DamageFrequencySeconds tick. At
    /// Legendary, also applies a movement slow to enemies inside.
    ///
    /// Like DarkShieldController, this isn't a cast-on-cooldown ability —
    /// it's a persistent state ticked independently of AbilityRuntime's
    /// cooldown model, which doesn't fit an always-on aura.
    ///
    /// Attach to: PlayerRoot.
    /// </summary>
    public class PoisonAuraController : MonoBehaviour
    {
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private Stats.StatSheet statSheet;

        private float _baseDamage;
        private float _damageFrequencySeconds;
        private float _range;
        private float _slowFraction;

        private float _tickTimer;
        private bool _isGranted;

        public bool IsGranted => _isGranted;

        private void Awake()
        {
            Debug.Assert(enemyPool, "PoisonAuraController: enemyPool not assigned.", this);
            Debug.Assert(statSheet, "PoisonAuraController: statSheet not assigned.", this);
        }

        private void Update()
        {
            if (!_isGranted) return;

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;

            _tickTimer = _damageFrequencySeconds;
            DamageAndSlowEnemiesInRange();
        }

        /// <summary>Grants or upgrades the aura to the given rarity's stats.</summary>
        public void GrantAura(Abilities.PoisonAuraStats stats)
        {
            _baseDamage = stats.Damage;
            _damageFrequencySeconds = stats.DamageFrequencySeconds;
            _range = stats.Range;
            _slowFraction = stats.SlowFraction;

            _isGranted = true;
            _tickTimer = 0f; // tick immediately on grant rather than waiting a full interval
        }

        private void DamageAndSlowEnemiesInRange()
        {
            // Ability Power affects "all ability damage" per the design doc —
            // applied here even though Poison Aura isn't an IAbility.Cast,
            // since it's still an ability for stat-scaling purposes.
            var scaledDamage = statSheet.ApplyPercentBonus(_baseDamage, Stats.StatType.AbilityPower);
            var slowMultiplier = 1f - _slowFraction; // 0 slow -> 1x speed, 0.5 slow -> 0.5x speed
            var playerPosition = transform.position;

            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(playerPosition, enemyView.Data.Position);
                if (distance > _range) continue;

                enemyView.TakeDamage(scaledDamage);

                if (_slowFraction > 0f)
                    enemyView.DataRef.ApplySlow(slowMultiplier, _damageFrequencySeconds * 1.5f);
            }
        }
    }
}
