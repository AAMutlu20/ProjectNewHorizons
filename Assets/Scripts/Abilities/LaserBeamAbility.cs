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

        public LaserBeamAbility(LaserBeamDefinitionSo definition, LaserBeamZonePool beamPool, Transform playerTransform)
        {
            _definition = definition;
            _beamPool = beamPool;
            _playerTransform = playerTransform;
        }

        public void Cast(Vector3 castOrigin, Rarity rarity, StatSheet statSheet, EnemyPool enemyPool)
        {
            var stats = _definition.GetStatsForRarity(rarity);
            var weakenMultiplier = 1f + stats.WeakeningMultiplierBonus;
            var rotationSpeed = rarity == Rarity.Legendary ? LegendaryRotationDegreesPerSecond : 0f;

            for (var i = 0; i < stats.BeamCount; i++)
            {
                var direction = CardinalDirections[Random.Range(0, CardinalDirections.Length)];
                _beamPool.Begin(_playerTransform, direction, BeamWidth, stats.DamagePercentMaxHpPerSecond,
                    weakenMultiplier, stats.Duration, rotationSpeed, enemyPool);
            }
        }
    }
}
