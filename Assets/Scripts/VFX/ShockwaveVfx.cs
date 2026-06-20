using Abilities;
using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays Shockwave's ground-slam particle effect, scaled to match the
    /// actual radius for whatever rarity was cast. Listens for
    /// AbilityCastEvent rather than being called directly by ShockwaveAbility —
    /// keeps gameplay logic and presentation fully decoupled, matching the
    /// project's existing pattern (ZombieExplodedEvent, EnemyBuffedEvent).
    ///
    /// Attach to: a GameObject in [Systems] or under PlayerRoot — position
    /// doesn't matter, it moves the particle system to the cast origin itself.
    /// </summary>
    public class ShockwaveVfx : MonoBehaviour
    {
        [SerializeField] private ShockwaveDefinitionSo shockwaveDefinition;
        [SerializeField] private ParticleSystem shockwaveParticles;

        [Tooltip("The particle effect's authored radius at scale 1 — used to compute " +
                 "the scale factor needed to match each rarity's actual Radius stat.")]
        [SerializeField] private float particleEffectBaseRadius = 1f;

        private void Awake()
        {
            Debug.Assert(shockwaveDefinition, "ShockwaveVfx: shockwaveDefinition not assigned.", this);
            Debug.Assert(shockwaveParticles, "ShockwaveVfx: shockwaveParticles not assigned.", this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AbilityCastEvent>(OnAbilityCast);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AbilityCastEvent>(OnAbilityCast);
        }

        private void OnAbilityCast(AbilityCastEvent abilityCast)
        {
            if (abilityCast.AbilityName != shockwaveDefinition.displayName) return;

            PlayAt(abilityCast.CastOrigin, abilityCast.Rarity);
        }

        private void PlayAt(Vector3 position, Stats.Rarity rarity)
        {
            var stats = shockwaveDefinition.GetStatsForRarity(rarity);
            var scaleFactor = particleEffectBaseRadius > 0f ? stats.Radius / particleEffectBaseRadius : 1f;

            shockwaveParticles.transform.position = position;
            shockwaveParticles.transform.localScale = Vector3.one * scaleFactor;
            shockwaveParticles.Play();
        }
    }
}
