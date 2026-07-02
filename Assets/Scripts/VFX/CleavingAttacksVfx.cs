using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays a slash/sweep particle effect when Cleaving Attacks triggers.
    /// Listens for CleavingTriggeredEvent — decoupled from CleavingAttacksEnchant.
    ///
    /// Attach to: PlayerRoot or a VFX anchor child.
    /// Wire: cleaveParticles.
    /// </summary>
    public class CleavingAttacksVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem cleaveParticles;

        private void OnEnable()  => EventBus.Subscribe<CleavingTriggeredEvent>(OnCleave);
        private void OnDisable() => EventBus.Unsubscribe<CleavingTriggeredEvent>(OnCleave);

        private void OnCleave(CleavingTriggeredEvent e)
        {
            if (!cleaveParticles) return;
            cleaveParticles.transform.position = e.SwingOrigin;
            cleaveParticles.Play();
        }
    }
}
