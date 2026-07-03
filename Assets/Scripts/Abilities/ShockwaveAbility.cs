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

        // Authored radius of the particle system at localScale = 1.
        // Set this to match your particle asset so the VFX matches the gameplay radius.
        private readonly float _particleBaseRadius;

        public ShockwaveAbility(ShockwaveDefinitionSo definition,
            UnityEngine.ParticleSystem particles = null, float particleBaseRadius = 1f)
        {
            _definition = definition;
            _particles = particles;
            _particleBaseRadius = particleBaseRadius;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var stats = _definition.GetStatsForRarity(rarity);
            var scaledDamage = statSheet.ApplyPercentBonus(stats.Damage, StatType.AbilityPower);

            PlayScaled(_particles, castOrigin, radius: stats.Radius, baseRadius: _particleBaseRadius);

            DamageAndStunEnemiesInRadius(castOrigin, stats.Radius, scaledDamage, stats.StunDuration, enemyPool);
        }

        // Scales the particle system so its visual radius matches the gameplay radius,
        // then plays it. Safe to call with a null particle — does nothing.
        private static void PlayScaled(UnityEngine.ParticleSystem ps, UnityEngine.Vector3 pos,
            float radius, float baseRadius)
        {
            if (!ps) return;
            ps.transform.position = pos;
            var scale = baseRadius > 0f ? radius / baseRadius : 1f;
            ps.transform.localScale = UnityEngine.Vector3.one * scale;
            ps.Play();
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
