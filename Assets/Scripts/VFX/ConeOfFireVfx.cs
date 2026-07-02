using Abilities;
using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays Cone of Fire's flame particle effect, oriented in the player's
    /// facing direction at the time of cast, scaled to match the rarity's Range.
    /// Listens for AbilityCastEvent — same pattern as ShockwaveVfx.
    ///
    /// Attach to: PlayerRoot or a VFX anchor child.
    /// Wire: coneOfFireDefinition, coneParticles.
    /// The particle system should be authored facing forward (+Z) at scale 1
    /// so rotation and scale can be applied cleanly at cast time.
    /// </summary>
    public class ConeOfFireVfx : MonoBehaviour
    {
        [SerializeField] private ConeOfFireDefinitionSo coneOfFireDefinition;
        [SerializeField] private ParticleSystem coneParticles;

        [Tooltip("Authored length of the particle effect at scale 1 — used to derive " +
                 "the scale factor so the VFX matches each rarity's Range.")]
        [SerializeField] private float particleBaseRange = 1f;

        [Tooltip("The player's transform — used to read FacingDirection at cast time " +
                 "since AbilityCastEvent doesn't carry the facing direction.")]
        [SerializeField] private Player.PlayerController playerController;

        private void Awake()
        {
            Debug.Assert(coneOfFireDefinition, "ConeOfFireVfx: definition not assigned.", this);
            Debug.Assert(coneParticles, "ConeOfFireVfx: coneParticles not assigned.", this);
            Debug.Assert(playerController, "ConeOfFireVfx: playerController not assigned.", this);
        }

        private void OnEnable()  => EventBus.Subscribe<AbilityCastEvent>(OnAbilityCast);
        private void OnDisable() => EventBus.Unsubscribe<AbilityCastEvent>(OnAbilityCast);

        private void OnAbilityCast(AbilityCastEvent e)
        {
            if (e.AbilityName != coneOfFireDefinition.displayName) return;

            var stats = coneOfFireDefinition.GetStatsForRarity(e.Rarity);
            var scaleFactor = particleBaseRange > 0f ? stats.Range / particleBaseRange : 1f;
            var facing = playerController.FacingDirection;

            coneParticles.transform.position = e.CastOrigin;
            coneParticles.transform.rotation = facing.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(facing)
                : Quaternion.identity;
            coneParticles.transform.localScale = Vector3.one * scaleFactor;
            coneParticles.Play();
        }
    }
}
