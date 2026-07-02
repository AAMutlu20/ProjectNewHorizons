using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays a particle effect each time the player's melee weapon lands a hit.
    /// Listens for MeleeHitEvent — fully decoupled from MeleeWeapon.
    /// Optionally plays a separate, larger effect on crits.
    ///
    /// Attach to: PlayerRoot or a VFX anchor child.
    /// Wire: hitParticles, optionally critParticles.
    /// </summary>
    public class MeleeHitVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem hitParticles;
        [SerializeField] private ParticleSystem critParticles; // optional — plain hit plays if null

        private void OnEnable()  => EventBus.Subscribe<MeleeHitEvent>(OnMeleeHit);
        private void OnDisable() => EventBus.Unsubscribe<MeleeHitEvent>(OnMeleeHit);

        private void OnMeleeHit(MeleeHitEvent e)
        {
            if (e.IsCrit && critParticles)
            {
                critParticles.transform.position = e.HitPosition;
                critParticles.Play();
            }
            else if (hitParticles)
            {
                hitParticles.transform.position = e.HitPosition;
                hitParticles.Play();
            }
        }
    }
}
