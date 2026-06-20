using Core;
using Player;
using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Plays a flash/particle effect when Dark Shield blocks a hit, and can
    /// toggle a persistent shield visual (e.g. an orbiting shell mesh or
    /// shader effect) based on whether the shield is currently active or
    /// fully depleted. Listens for ShieldBlockedDamageEvent / ShieldChangedEvent
    /// rather than being called directly by DarkShieldController.
    ///
    /// Attach to: PlayerRoot or a child VFX anchor on the player.
    /// </summary>
    public class ShieldVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem blockFlashParticles;
        [SerializeField] private GameObject persistentShieldVisual;

        private void OnEnable()
        {
            EventBus.Subscribe<ShieldBlockedDamageEvent>(OnShieldBlockedDamage);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ShieldBlockedDamageEvent>(OnShieldBlockedDamage);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void OnShieldBlockedDamage(ShieldBlockedDamageEvent blockedDamage)
        {
            if (blockFlashParticles) blockFlashParticles.Play();
        }

        private void OnShieldChanged(ShieldChangedEvent shieldChanged)
        {
            if (!persistentShieldVisual) return;
            persistentShieldVisual.SetActive(shieldChanged.CurrentLayers > 0);
        }
    }
}
