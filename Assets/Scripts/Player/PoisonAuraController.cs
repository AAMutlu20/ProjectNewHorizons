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

        [SerializeField] private GameObject poisonAuraVisual;
        [SerializeField] private UnityEngine.ParticleSystem auraParticles;

        private float _baseDamage;
        private float _damageFrequencySeconds;
        private float _range;
        private float _slowFraction;

        private float _tickTimer;
        private bool _isGranted;

        // The poison particle system starts and stops by this setting.
        public bool IsGranted { get { return _isGranted; } set { _isGranted = value; } }

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

            // Enable visual
            poisonAuraVisual.SetActive(true);
            if (auraParticles) auraParticles.Play();
            Audio.AudioManager.Instance?.SetPoisonAuraLoop(true);

            _isGranted = true;
            _tickTimer = 0f; // tick immediately on grant rather than waiting a full interval
        }

        private void DamageAndSlowEnemiesInRange()
        {
            // Ability Power affects "all ability damage" per the design doc —
            // applied here even though Poison Aura isn't an IAbility.Cast,
            // since it's still an ability for stat-scaling purposes.
            var scaledDamage = statSheet.ApplyPercentBonus(_baseDamage, Stats.StatType.AbilityPower);
            var playerPosition = transform.position;

            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(playerPosition, enemyView.Data.Position);
                if (distance > _range) continue;

                enemyView.TakeDamage(scaledDamage);

                // Raw fraction (e.g. 0.5 for "50% slower"), NOT a pre-inverted
                // speed multiplier -- EnemyData.ApplySlow applies the
                // "1 - fraction * stackMultiplier" math itself as part of the
                // stacking rework.
                if (_slowFraction > 0f)
                    enemyView.DataRef.ApplySlow(_slowFraction, _damageFrequencySeconds * 1.5f);
            }
        }
    }
}
