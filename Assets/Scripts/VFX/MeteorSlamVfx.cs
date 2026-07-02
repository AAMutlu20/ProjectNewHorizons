using Abilities;
using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays Meteor Slam's impact particle effect when the meteor lands.
    /// The telegraph ring and lava pool zone already handle their own visuals
    /// (AoeTelegraphRingPool and DamageOverTimeZonePool respectively) —
    /// this script covers the impact burst at the moment of landing.
    ///
    /// The impact moment is when AbilityCastEvent fires (which is after
    /// MeteorSlamAbility.Cast, which starts the telegraph that calls
    /// LandMeteor on completion). To play at LANDING rather than cast,
    /// we wait for the telegraph duration. Alternatively, connect impact
    /// particles directly as a callback on AoeTelegraphRingPool.Begin's
    /// onComplete if you need exact timing — see MeteorSlamAbility.
    ///
    /// Attach to: PlayerRoot or a VFX anchor child.
    /// Wire: meteorSlamDefinition, impactParticles.
    /// </summary>
    public class MeteorSlamVfx : MonoBehaviour
    {
        [SerializeField] private MeteorSlamDefinitionSo meteorSlamDefinition;
        [SerializeField] private ParticleSystem impactParticles;

        [Tooltip("Match this to AoeTelegraphRing's fill duration so the impact plays when the meteor lands.")]
        [SerializeField] private float telegraphDuration = 0.8f;

        [SerializeField] private float particleBaseRadius = 1f;

        private void Awake()
        {
            Debug.Assert(meteorSlamDefinition, "MeteorSlamVfx: definition not assigned.", this);
            Debug.Assert(impactParticles, "MeteorSlamVfx: impactParticles not assigned.", this);
        }

        private void OnEnable()  => EventBus.Subscribe<AbilityCastEvent>(OnAbilityCast);
        private void OnDisable() => EventBus.Unsubscribe<AbilityCastEvent>(OnAbilityCast);

        private void OnAbilityCast(AbilityCastEvent e)
        {
            if (e.AbilityName != meteorSlamDefinition.displayName) return;

            var stats = meteorSlamDefinition.GetStatsForRarity(e.Rarity);
            var scaleFactor = particleBaseRadius > 0f ? stats.ImpactRadius / particleBaseRadius : 1f;
            var castOrigin = e.CastOrigin; // capture for lambda

            // Delay the impact VFX by the telegraph duration so it fires when the meteor lands.
            StartCoroutine(PlayImpactAfterDelay(castOrigin, scaleFactor, telegraphDuration));
        }

        private System.Collections.IEnumerator PlayImpactAfterDelay(Vector3 position, float scale, float delay)
        {
            yield return new WaitForSeconds(delay);
            impactParticles.transform.position = position;
            impactParticles.transform.localScale = Vector3.one * scale;
            impactParticles.Play();
        }
    }
}
