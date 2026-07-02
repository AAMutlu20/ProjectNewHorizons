using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Dark Shield: blocks the player's next hit entirely, consuming one
    /// layer per block. Layers regenerate individually 30 seconds after being
    /// lost — unless the shield is fully depleted, in which case it disables
    /// completely and takes 60 seconds to reform with all layers restored at
    /// once, per the design doc.
    ///
    /// Unlike Shockwave, this isn't a cast-on-cooldown ability — it's a
    /// passive blocking state PlayerHealth checks before applying damage.
    /// Granted via GrantShield(rarity) from the level-up/ability-choice flow,
    /// same as other abilities, but ticked here independently rather than
    /// through AbilityRuntime's cooldown model, which doesn't fit this shape.
    ///
    /// Attach to: PlayerRoot.
    /// </summary>
    public class DarkShieldController : MonoBehaviour
    {
        [SerializeField] private GameObject darkShieldVisual;
        [SerializeField] private UnityEngine.ParticleSystem blockParticles;

        private int _maxLayers;
        private int _currentLayers;
        private float _layerRegenSeconds;
        private float _fullShieldRegenSeconds;

        // One countdown per currently-missing layer, tracked independently —
        // losing layer 2 then layer 3 a moment later means each regenerates
        // 30s after its own loss, not both at once.
        private readonly List<float> _layerRegenTimers = new();

        // Active only while the shield is fully depleted (0 layers). While
        // this is running, individual layer regen is paused — the doc treats
        // "fully destroyed" as a distinct state from "missing some layers."
        private float _fullShieldRegenTimer;
        private bool _isFullyDepleted;

        public bool IsGranted => _maxLayers > 0;
        public int CurrentLayers => _currentLayers;

        private void Update()
        {
            if (!IsGranted) return;

            if (_isFullyDepleted)
                TickFullShieldRegen();
            else
                TickIndividualLayerRegen();
        }

        /// <summary>Grants or upgrades the shield to the given rarity's layer count. Resets to full on grant.</summary>
        public void GrantShield(Abilities.DarkShieldStats stats)
        {
            _maxLayers = stats.Layers;
            _currentLayers = stats.Layers;
            _layerRegenSeconds = stats.LayerRegenSeconds;
            _fullShieldRegenSeconds = stats.FullShieldRegenSeconds;
            // Activate visual
            darkShieldVisual.SetActive(true);

            _layerRegenTimers.Clear();
            _isFullyDepleted = false;

            EmitShieldChanged();
        }

        /// <summary>
        /// Called by PlayerHealth before applying damage. If a layer is
        /// available, consumes it and returns true (damage fully blocked).
        /// Returns false if the shield isn't granted or has no layers left.
        /// </summary>
        public bool TryBlockDamage()
        {
            if (!IsGranted || _currentLayers <= 0) return false;

            _currentLayers--;
            EmitShieldChanged();

            if (_currentLayers <= 0)
                BeginFullShieldRegen();
            else
                _layerRegenTimers.Add(_layerRegenSeconds);

            if (blockParticles) blockParticles.Play();
            EventBus.Emit(new ShieldBlockedDamageEvent { RemainingLayers = _currentLayers });
            return true;
        }

        private void BeginFullShieldRegen()
        {
            _isFullyDepleted = true;
            _fullShieldRegenTimer = _fullShieldRegenSeconds;
            _layerRegenTimers.Clear(); // individual timers don't matter once fully depleted
        }

        private void TickFullShieldRegen()
        {
            _fullShieldRegenTimer -= Time.deltaTime;
            if (_fullShieldRegenTimer > 0f) return;

            _isFullyDepleted = false;
            _currentLayers = _maxLayers;
            EmitShieldChanged();
        }

        private void TickIndividualLayerRegen()
        {
            for (var i = _layerRegenTimers.Count - 1; i >= 0; i--)
            {
                _layerRegenTimers[i] -= Time.deltaTime;
                if (_layerRegenTimers[i] > 0f) continue;

                _layerRegenTimers.RemoveAt(i);
                _currentLayers = Mathf.Min(_maxLayers, _currentLayers + 1);
                EmitShieldChanged();
            }
        }

        private void EmitShieldChanged()
        {
            EventBus.Emit(new ShieldChangedEvent { CurrentLayers = _currentLayers, MaxLayers = _maxLayers });
        }
    }

    /// <summary>Emitted whenever the shield's layer count changes — drives the shield HUD icon/pips.</summary>
    public struct ShieldChangedEvent
    {
        public int CurrentLayers;
        public int MaxLayers;
    }

    /// <summary>Emitted each time the shield actually blocks a hit — for VFX (a shield-flash effect).</summary>
    public struct ShieldBlockedDamageEvent
    {
        public int RemainingLayers;
    }
}
