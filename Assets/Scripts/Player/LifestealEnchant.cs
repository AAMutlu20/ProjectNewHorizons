using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Lifesteal melee enchant: heals the player for DamageHealPercent of
    /// each landed hit's damage. At Legendary, also permanently raises max
    /// HP by a tiny flat amount per hit.
    ///
    /// Attach to: the same GameObject as MeleeWeapon (needs PlayerHealth on
    /// a parent, same lookup pattern as MeleeWeapon's StatSheet reference).
    /// </summary>
    public class LifestealEnchant : MonoBehaviour, IMeleeEnchant
    {
        private const float PercentToFraction = 100f;

        [SerializeField] private Abilities.LifestealDefinitionSo definition;
        [SerializeField] private UnityEngine.ParticleSystem healParticles;

        private PlayerHealth _playerHealth;
        private float _healPercent;
        private float _permanentHpIncreasePerHit;
        private bool _isGranted;

        private void Awake()
        {
            _playerHealth = GetComponentInParent<PlayerHealth>();
            Debug.Assert(definition, "LifestealEnchant: definition not assigned.", this);
            Debug.Assert(_playerHealth, "LifestealEnchant: no PlayerHealth found in parent hierarchy.", this);
        }

        public void GrantEnchant(Stats.Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            _healPercent = stats.DamageHealPercent;
            _permanentHpIncreasePerHit = stats.PermanentHpIncreasePerHit;
            _isGranted = true;
        }

        public void OnMeleeHit(EnemyView target, float damageDealt, Vector3 hitOrigin)
        {
            if (!_isGranted) return;

            var healAmount = damageDealt * (_healPercent / PercentToFraction);
            _playerHealth.Heal(healAmount);
            if (healParticles) healParticles.Play();

            if (_permanentHpIncreasePerHit > 0f)
                _playerHealth.IncreaseMaxHpPermanently(_permanentHpIncreasePerHit);
        }
    }
}
