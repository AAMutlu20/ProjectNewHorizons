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
        [SerializeField] private UnityEngine.ParticleSystem shockwaveParticles;
        [Tooltip("Authored radius of shockwaveParticles at localScale=1. The script scales it to match the rarity radius.")]
        [SerializeField] private float shockwaveParticleBaseRadius = 1f;

        [Header("Meteor Slam")]
        [SerializeField] private MeteorSlamDefinitionSo meteorSlamDefinition;
        [SerializeField] private AoeTelegraphRingPool meteorSlamTelegraphPool;
        [SerializeField] private VFX.DamageOverTimeZonePool meteorSlamLavaPoolPool;
        [SerializeField] private MeteorSlamChoiceEntry meteorSlamEntry;
        [SerializeField] private UnityEngine.ParticleSystem meteorSlamParticles;
        [SerializeField] private float meteorSlamParticleBaseRadius = 1f;

        [Header("Laser Beam")]
        [SerializeField] private LaserBeamDefinitionSo laserBeamDefinition;
        [SerializeField] private VFX.LaserBeamZonePool laserBeamZonePool;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private LaserBeamChoiceEntry laserBeamEntry;
        [SerializeField] private UnityEngine.ParticleSystem laserBeamParticles;
        [SerializeField] private float laserBeamParticleBaseWidth = 1f;

        [Header("Cone of Fire")]
        [SerializeField] private ConeOfFireDefinitionSo coneOfFireDefinition;
        [SerializeField] private Player.PlayerController playerController;
        [SerializeField] private ConeOfFireChoiceEntry coneOfFireEntry;
        [SerializeField] private UnityEngine.ParticleSystem coneOfFireParticles;
        [SerializeField] private float coneOfFireParticleBaseRange = 1f;

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

            var ability = new ShockwaveAbility(shockwaveDefinition, shockwaveParticles, shockwaveParticleBaseRadius);
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

            var ability = new MeteorSlamAbility(meteorSlamDefinition, meteorSlamTelegraphPool, meteorSlamLavaPoolPool, meteorSlamParticles, meteorSlamParticleBaseRadius);
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

            var ability = new LaserBeamAbility(laserBeamDefinition, laserBeamZonePool, playerTransform, laserBeamParticles, laserBeamParticleBaseWidth);
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

            var ability = new ConeOfFireAbility(coneOfFireDefinition, playerController, coneOfFireParticles, coneOfFireParticleBaseRange);
            coneOfFireEntry.Configure(ability,
                stats => $"Damage: {stats.Damage:F0}  Range: {stats.Range:F0}");
        }
    }
}
