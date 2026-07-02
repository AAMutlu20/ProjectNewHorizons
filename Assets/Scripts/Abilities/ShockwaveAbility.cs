using Enemies;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Shockwave: the player slams the ground, damaging and stunning every
    /// enemy within radius. Ability Power scales the damage (percent-additive,
    /// per the design doc) — radius and stun duration are not affected by
    /// Ability Power, since the doc only specifies "affecting all ability
    /// damage," not area or duration.
    /// </summary>
    public class ShockwaveAbility : IAbility
    {
        private readonly ShockwaveDefinitionSo _definition;
        private readonly UnityEngine.ParticleSystem _particles;

        public ShockwaveAbility(ShockwaveDefinitionSo definition, UnityEngine.ParticleSystem particles = null)
        {
            _definition = definition;
            _particles = particles;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var stats = _definition.GetStatsForRarity(rarity);
            var scaledDamage = statSheet.ApplyPercentBonus(stats.Damage, StatType.AbilityPower);

            if (_particles)
            {
                _particles.transform.position = castOrigin;
                _particles.Play();
            }

            DamageAndStunEnemiesInRadius(castOrigin, stats.Radius, scaledDamage, stats.StunDuration, enemyPool);
        }

        private static void DamageAndStunEnemiesInRadius(
            Vector3 origin, float radius, float damage, float stunDuration, EnemyPool enemyPool)
        {
            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(origin, enemyView.Data.Position);
                if (distance > radius) continue;

                enemyView.TakeDamage(damage, origin);
                enemyView.DataRef.ApplyStun(stunDuration);
            }
        }
    }
}
