using Enemies;
using Stats;
using UnityEngine;
using VFX;

namespace Abilities
{
    /// <summary>
    /// Meteor Slam: targets the enemy with the most current HP, telegraphs
    /// at their position, then deals AOE damage to it and everyone within
    /// ImpactRadius on landing. At Epic/Legendary, also leaves a lava pool
    /// DOT zone that damages each enemy inside it for a percentage of THAT
    /// enemy's own max HP per second — not a flat number.
    ///
    /// If no enemies are alive when this casts, the cast is skipped entirely —
    /// the doc only describes targeting "the enemy," with no random-position
    /// fallback specified.
    /// </summary>
    public class MeteorSlamAbility : IAbility
    {
        private const float TelegraphDuration = 0.8f;
        private const float LavaPoolTickInterval = 1f; // doc's "X% maxhp/s" implies a per-second tick

        private readonly MeteorSlamDefinitionSo _definition;
        private readonly AoeTelegraphRingPool _telegraphPool;
        private readonly DamageOverTimeZonePool _lavaPoolPool;
        private readonly UnityEngine.ParticleSystem _particles;
        private readonly float _particleBaseRadius;

        public MeteorSlamAbility(MeteorSlamDefinitionSo definition, AoeTelegraphRingPool telegraphPool,
            DamageOverTimeZonePool lavaPoolPool, UnityEngine.ParticleSystem particles = null,
            float particleBaseRadius = 1f)
        {
            _definition = definition;
            _telegraphPool = telegraphPool;
            _lavaPoolPool = lavaPoolPool;
            _particles = particles;
            _particleBaseRadius = particleBaseRadius;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var target = FindHighestHpEnemy(enemyPool);
            if (target == null) return; // no valid target — doc specifies no fallback behaviour

            var stats = _definition.GetStatsForRarity(rarity);
            var scaledImpactDamage = statSheet.ApplyPercentBonus(stats.Damage, StatType.AbilityPower);

            // Ability Power affects "all ability damage" per the design doc —
            // applied to the lava pool's percent-maxHP rate too, not just the
            // flat impact hit.
            var scaledLavaPoolPercent = statSheet.ApplyPercentBonus(
                stats.LavaPoolDamagePercentPerSecond, StatType.AbilityPower);

            var impactPosition = target.Data.Position;

            _telegraphPool.Begin(impactPosition, TelegraphDuration, stats.ImpactRadius,
                () => LandMeteor(impactPosition, stats, scaledImpactDamage, scaledLavaPoolPercent, enemyPool));
        }

        private static EnemyView FindHighestHpEnemy(EnemyPool enemyPool)
        {
            EnemyView highestHpEnemy = null;
            var highestHp = float.MinValue;

            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;
                if (enemyView.Data.Hp <= highestHp) continue;

                highestHp = enemyView.Data.Hp;
                highestHpEnemy = enemyView;
            }

            return highestHpEnemy;
        }

        private void LandMeteor(Vector3 impactPosition, MeteorSlamStats stats, float damage,
            float lavaPoolDamagePercent, EnemyPool enemyPool)
        {
            if (_particles)
            {
                var scale = _particleBaseRadius > 0f ? stats.ImpactRadius / _particleBaseRadius : 1f;
                _particles.transform.position = impactPosition;
                _particles.transform.localScale = UnityEngine.Vector3.one * scale;
                _particles.Play();
            }

            DamageEnemiesInRadius(impactPosition, stats.ImpactRadius, damage, enemyPool);

            if (stats.LavaPoolDuration <= 0f) return; // Base/Rare have no lava pool

            _lavaPoolPool.Begin(
                impactPosition,
                stats.LavaPoolRange,
                LavaPoolTickInterval,
                stats.LavaPoolDuration,
                enemyView => enemyView.Data.MaxHp * lavaPoolDamagePercent,
                enemyPool);
        }

        private static void DamageEnemiesInRadius(Vector3 impactPosition, float radius, float damage, EnemyPool enemyPool)
        {
            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;

                var distance = Vector3.Distance(impactPosition, enemyView.Data.Position);
                if (distance > radius) continue;

                enemyView.TakeDamage(damage, impactPosition);
            }
        }
    }
}
