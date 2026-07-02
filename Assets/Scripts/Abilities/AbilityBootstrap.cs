using UnityEngine;
using VFX;

namespace Abilities
{
    /// <summary>
    /// Constructs every cooldown-cast IAbility (Shockwave, Meteor Slam, Laser
    /// Beam, Cone of Fire) and calls Configure() on its CooldownAbilityChoiceEntry
    /// wrapper, once at scene start. This step can't be done through the
    /// Inspector alone -- IAbility instances need scene-specific pool
    /// references constructed in code (e.g. new ShockwaveAbility(definition)),
    /// not serializable as plain fields.
    ///
    /// Runs in Awake, before LevelSystem's first level-up could possibly fire,
    /// so every wrapper is Configure()'d before anything tries to Grant() it.
    ///
    /// Attach to: a GameObject in [Systems], alongside the rest of the
    /// ability roster. Wire every field in the Inspector -- the four
    /// CooldownAbilityChoiceEntry&lt;TStats&gt; components (one per ability) plus
    /// each ability's own dependencies (pools, definitions, player references).
    /// </summary>
    public class AbilityBootstrap : MonoBehaviour
    {
        [Header("Shockwave")]
        [SerializeField] private ShockwaveDefinitionSo shockwaveDefinition;
        [SerializeField] private ShockwaveChoiceEntry shockwaveEntry;

        [Header("Meteor Slam")]
        [SerializeField] private MeteorSlamDefinitionSo meteorSlamDefinition;
        [SerializeField] private AoeTelegraphRingPool meteorSlamTelegraphPool;
        [SerializeField] private DamageOverTimeZonePool meteorSlamLavaPoolPool;
        [SerializeField] private MeteorSlamChoiceEntry meteorSlamEntry;

        [Header("Laser Beam")]
        [SerializeField] private LaserBeamDefinitionSo laserBeamDefinition;
        [SerializeField] private LaserBeamZonePool laserBeamZonePool;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private LaserBeamChoiceEntry laserBeamEntry;

        [Header("Cone of Fire")]
        [SerializeField] private ConeOfFireDefinitionSo coneOfFireDefinition;
        [SerializeField] private Player.PlayerController playerController;
        [SerializeField] private ConeOfFireChoiceEntry coneOfFireEntry;

        private void Awake()
        {
            ConfigureShockwave();
            ConfigureMeteorSlam();
            ConfigureLaserBeam();
            ConfigureConeOfFire();
        }

        private void ConfigureShockwave()
        {
            if (!shockwaveDefinition || !shockwaveEntry)
            {
                Debug.LogError("AbilityBootstrap: Shockwave dependencies not fully assigned.", this);
                return;
            }

            var ability = new ShockwaveAbility(shockwaveDefinition);
            shockwaveEntry.Configure(ability,
                stats => $"Damage: {stats.Damage:F0}  Radius: {stats.Radius:F0}  Stun: {stats.StunDuration:F1}s");
        }

        private void ConfigureMeteorSlam()
        {
            if (!meteorSlamDefinition || !meteorSlamTelegraphPool || !meteorSlamLavaPoolPool || !meteorSlamEntry)
            {
                Debug.LogError("AbilityBootstrap: Meteor Slam dependencies not fully assigned.", this);
                return;
            }

            var ability = new MeteorSlamAbility(meteorSlamDefinition, meteorSlamTelegraphPool, meteorSlamLavaPoolPool);
            meteorSlamEntry.Configure(ability,
                stats => $"Damage: {stats.Damage:F0}  Impact Radius: {stats.ImpactRadius:F0}");
        }

        private void ConfigureLaserBeam()
        {
            if (!laserBeamDefinition || !laserBeamZonePool || !playerTransform || !laserBeamEntry)
            {
                Debug.LogError("AbilityBootstrap: Laser Beam dependencies not fully assigned.", this);
                return;
            }

            var ability = new LaserBeamAbility(laserBeamDefinition, laserBeamZonePool, playerTransform);
            laserBeamEntry.Configure(ability,
                stats => $"Beams: {stats.BeamCount}  Weakening: {stats.WeakeningMultiplierBonus * 100f:F0}%");
        }

        private void ConfigureConeOfFire()
        {
            if (!coneOfFireDefinition || !playerController || !coneOfFireEntry)
            {
                Debug.LogError("AbilityBootstrap: Cone of Fire dependencies not fully assigned.", this);
                return;
            }

            var ability = new ConeOfFireAbility(coneOfFireDefinition, playerController);
            coneOfFireEntry.Configure(ability,
                stats => $"Damage: {stats.Damage:F0}  Range: {stats.Range:F0}");
        }
    }
}
