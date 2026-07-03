using Enemies;
using Stats;
using UnityEngine;
using VFX;

namespace Abilities
{
    /// <summary>
    /// Laser Beam: spawns BeamCount beams, each in an independently-rolled
    /// random cardinal direction (N/S/E/W) from the player, infinite range,
    /// damaging and weakening everyone inside for Duration. At Legendary,
    /// beams slowly rotate throughout their duration.
    ///
    /// Needs the player's Transform (not just a position snapshot) since the
    /// beam follows the player as they move — injected via constructor
    /// rather than threaded through IAbility.Cast, which only provides a
    /// point-in-time castOrigin.
    /// </summary>
    public class LaserBeamAbility : IAbility
    {
        // The doc doesn't specify a beam width — this is a reasonable design
        // default; tune once tested in-game with real enemy/player scale.
        private const float BeamWidth = 1.5f;

        // Doc: "When Legendary, the beams start spinning slowly throughout
        // the duration." Doc gives no explicit speed — a slow, readable rotation.
        private const float LegendaryRotationDegreesPerSecond = 15f;

        private static readonly Vector3[] CardinalDirections =
        {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
        };

        private readonly LaserBeamDefinitionSo _definition;
        private readonly LaserBeamZonePool _beamPool;
        private readonly Transform _playerTransform;
        private readonly UnityEngine.ParticleSystem _particles;
        private readonly float _particleBaseWidth;

        public LaserBeamAbility(LaserBeamDefinitionSo definition, LaserBeamZonePool beamPool,
            Transform playerTransform, UnityEngine.ParticleSystem particles = null,
            float particleBaseWidth = 1f)
        {
            _definition = definition;
            _beamPool = beamPool;
            _playerTransform = playerTransform;
            _particles = particles;
            _particleBaseWidth = particleBaseWidth;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            if (_particles)
            {
                // Scale X/Z by beam width so the particle matches the gameplay hitbox width.
                // Y scale 1 — beam is infinite length, so length isn't author-scaled here;
                // use a long particle system emission shape authored at your desired length.
                var widthScale = _particleBaseWidth > 0f ? BeamWidth / _particleBaseWidth : 1f;
                _particles.transform.position = castOrigin;
                _particles.transform.localScale = new UnityEngine.Vector3(widthScale, 1f, widthScale);
                _particles.Play();
            }

            var stats = _definition.GetStatsForRarity(rarity);
            // Raw fraction (e.g. 0.10 for +10% damage taken), NOT 1+fraction --
            // EnemyData.ApplyWeaken now applies the "1 + fraction * stackMultiplier"
            // math itself as part of the stacking rework, so this must pass the
            // bare fraction, not a pre-added multiplier.
            var weakenFraction = stats.WeakeningMultiplierBonus;
            var rotationSpeed = rarity == Rarity.Legendary ? LegendaryRotationDegreesPerSecond : 0f;

            // Ability Power affects "all ability damage" per the design doc —
            // applied to the percent-maxHP rate itself, same mechanism as
            // every flat-damage ability, since it's still ability damage.
            var scaledDamagePercent = statSheet.ApplyPercentBonus(
                stats.DamagePercentMaxHpPerSecond, StatType.AbilityPower);

            for (var i = 0; i < stats.BeamCount; i++)
            {
                var direction = CardinalDirections[Random.Range(0, CardinalDirections.Length)];
                _beamPool.Begin(_playerTransform, direction, BeamWidth, scaledDamagePercent,
                    weakenFraction, stats.Duration, rotationSpeed, enemyPool);
            }
        }
    }
}
