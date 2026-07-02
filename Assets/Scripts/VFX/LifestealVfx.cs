using Core;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays a healing/drain effect each time Lifesteal triggers on a melee hit.
    /// Listens for LifestealHealEvent — decoupled from LifestealEnchant.
    ///
    /// Attach to: PlayerRoot or a VFX anchor child.
    /// Wire: healParticles.
    /// </summary>
    public class LifestealVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem healParticles;

        private void OnEnable()  => EventBus.Subscribe<LifestealHealEvent>(OnLifesteal);
        private void OnDisable() => EventBus.Unsubscribe<LifestealHealEvent>(OnLifesteal);

        private void OnLifesteal(LifestealHealEvent e)
        {
            if (!healParticles) return;
            healParticles.transform.position = e.HitPosition;
            healParticles.Play();
        }
    }
}
