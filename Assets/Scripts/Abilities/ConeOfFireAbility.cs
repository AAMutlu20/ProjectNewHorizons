using Enemies;
using Player;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Cone of Fire: instantly damages every alive enemy within Range and
    /// within a forward-facing cone (using PlayerController.FacingDirection,
    /// since the player has no rotation/aim of their own -- see that
    /// property's doc comment). Any hit enemy that SURVIVES the initial hit
    /// is set on fire -- a per-enemy burn debuff (EnemyData.ApplyBurn) dealing
    /// ResidualDamagePerSecond for ResidualDuration, the same mechanism as
    /// Cleaving Attacks' bleed but with flat (not percent-of-maxHP) damage.
    ///
    /// This is NOT a ground-based DOT zone -- "residual fire damage" in the
    /// doc means the TARGET is burning, not that the player drops a patch of
    /// fire on the ground. No DamageOverTimeZonePool dependency needed.
    /// </summary>
    public class ConeOfFireAbility : IAbility
    {
        // The doc gives Range but not a cone angle -- half-angle of 45 degrees
        // (90 degree total cone width) is a reasonable melee-adjacent-AOE
        // default; tune once tested in-game.
        private const float ConeHalfAngleDegrees = 45f;

        private readonly ConeOfFireDefinitionSo _definition;
        private readonly PlayerController _playerController;

        public ConeOfFireAbility(ConeOfFireDefinitionSo definition, PlayerController playerController)
        {
            _definition = definition;
            _playerController = playerController;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var stats = _definition.GetStatsForRarity(rarity);
            var scaledDamage = statSheet.ApplyPercentBonus(stats.Damage, StatType.AbilityPower);
            var scaledBurnDamage = statSheet.ApplyPercentBonus(stats.ResidualDamagePerSecond, StatType.AbilityPower);
            var facingDirection = _playerController.FacingDirection;

            DamageAndBurnEnemiesInCone(castOrigin, facingDirection, stats.Range, scaledDamage,
                scaledBurnDamage, stats.ResidualDuration, enemyPool);
        }

        private static void DamageAndBurnEnemiesInCone(Vector3 origin, Vector3 facingDirection, float range,
            float damage, float burnDamagePerSecond, float burnDuration, EnemyPool enemyPool)
        {
            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (!enemyView || !enemyView.Data.IsAlive) continue;
                if (!IsInsideCone(origin, facingDirection, range, enemyView.Data.Position)) continue;

                enemyView.TakeDamage(damage, origin);

                // Doc: "scorching enemies and dealing residual fire damage over
                // time" -- only enemies that survive the initial hit catch fire,
                // same survives-the-hit gate as Cleaving Attacks' bleed.
                if (!enemyView.Data.IsAlive) continue;
                if (burnDuration <= 0f) continue;

                enemyView.DataRef.ApplyBurn(burnDamagePerSecond, burnDuration);
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
    }
}
