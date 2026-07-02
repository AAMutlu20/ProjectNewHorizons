using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Knockback melee enchant: defines the TOTAL knockback force for melee
    /// hits once granted (the doc's KnockbackIs values read as totals per
    /// tier, not additive bonuses on top of a separate base) — overrides
    /// MeleeWeapon's baseKnockbackForce rather than adding to it.
    ///
    /// Attach to: the same GameObject as MeleeWeapon.
    /// </summary>
    public class KnockbackEnchant : MonoBehaviour, IMeleeEnchant
    {
        [SerializeField] private Abilities.KnockbackDefinitionSo definition;
        [SerializeField] private UnityEngine.ParticleSystem knockbackParticles;

        private float _knockbackForce;
        private bool _isGranted;

        private void Awake()
        {
            Debug.Assert(definition, "KnockbackEnchant: definition not assigned.", this);
        }

        public void GrantEnchant(Stats.Rarity rarity)
        {
            var stats = definition.GetStatsForRarity(rarity);
            _knockbackForce = stats.KnockbackForce;
            _isGranted = true;
        }

        public void OnMeleeHit(EnemyView target, float damageDealt, Vector3 hitOrigin)
        {
            if (!_isGranted) return;
            if (knockbackParticles)
            {
                knockbackParticles.transform.position = hitOrigin;
                knockbackParticles.Play();
            }
        }

        public float? GetKnockbackForceOverride() => _isGranted ? _knockbackForce : null;
    }
}
