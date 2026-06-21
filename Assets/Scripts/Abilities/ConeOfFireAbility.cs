using Enemies;
using Player;
using Stats;
using UnityEngine;
using VFX;

namespace Abilities
{
    /// <summary>
    /// Cone of Fire: instantly damages every alive enemy within Range and
    /// within a forward-facing cone (using PlayerController.FacingDirection,
    /// since the player has no rotation/aim of their own — see that
    /// property's doc comment). Then leaves a residual DOT zone for
    /// ResidualDuration, dealing ResidualDamagePerSecond per second.
    ///
    /// The residual zone is a CIRCLE centred on the player, not a cone — the
    /// doc doesn't specify the residual zone's shape, only that it deals
    /// "residual fire damage over time." A precise cone-shaped DOT zone
    /// would need its own geometry class; this reuses the existing circular
    /// DamageOverTimeZone as a reasonable approximation.
    /// </summary>
    public class ConeOfFireAbility : IAbility
    {
        private const float TickInterval = 1f; // matches DamageOverTimeZone's per-second convention

        // The doc gives Range but not a cone angle — half-angle of 45° (90°
        // total cone width) is a reasonable melee-adjacent-AOE default; tune
        // once tested in-game.
        private const float ConeHalfAngleDegrees = 45f;

        private readonly ConeOfFireDefinitionSo _definition;
        private readonly DamageOverTimeZonePool _residualZonePool;
        private readonly PlayerController _playerController;

        public ConeOfFireAbility(ConeOfFireDefinitionSo definition, DamageOverTimeZonePool residualZonePool,
            PlayerController playerController)
        {
            _definition = definition;
            _residualZonePool = residualZonePool;
            _playerController = playerController;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var stats = _definition.GetStatsForRarity(rarity);
            var scaledDamage = statSheet.ApplyPercentBonus(stats.Damage, StatType.AbilityPower);
            var scaledResidualDamage = statSheet.ApplyPercentBonus(stats.ResidualDamagePerSecond, StatType.AbilityPower);
            var facingDirection = _playerController.FacingDirection;

            DamageEnemiesInCone(castOrigin, facingDirection, stats.Range, scaledDamage, enemyPool);
            BeginResidualZone(castOrigin, stats.Range, scaledResidualDamage, stats.ResidualDuration, enemyPool);
        }

        private static void DamageEnemiesInCone(
            Vector3 origin, Vector3 facingDirection, float range, float damage, EnemyPool enemyPool)
        {
            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;
                if (!IsInsideCone(origin, facingDirection, range, enemyView.Data.Position)) continue;

                enemyView.TakeDamage(damage, origin);
            }
        }

        private static bool IsInsideCone(Vector3 origin, Vector3 facingDirection, float range, Vector3 targetPosition)
        {
            var toTarget = targetPosition - origin;
            var distance = toTarget.magnitude;
            if (distance > range) return false;
            if (distance <= 0.0001f) return true; // standing exactly on the origin counts as hit

            var angleToTarget = Vector3.Angle(facingDirection, toTarget);
            return angleToTarget <= ConeHalfAngleDegrees;
        }

        private void BeginResidualZone(
            Vector3 origin, float radius, float damagePerSecond, float duration, EnemyPool enemyPool)
        {
            if (duration <= 0f) return;

            _residualZonePool.Begin(origin, radius, TickInterval, duration,
                _ => damagePerSecond, enemyPool);
        }
    }
}
