using System.Collections.Generic;
using Player;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Owns every ability the player has picked up and ticks their cooldowns
    /// each frame, casting automatically — per the design doc, abilities are
    /// "special attacks that the player automatically, periodically performs."
    ///
    /// Attach to: PlayerRoot, alongside StatSheet and PlayerController.
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAbilityManager : MonoBehaviour
    {
        private readonly List<AbilityRuntime> _activeAbilities = new();

        private StatSheet _statSheet;
        private PlayerController _playerController;

        public IReadOnlyList<AbilityRuntime> ActiveAbilities => _activeAbilities;

        private void Awake()
        {
            _statSheet = GetComponent<StatSheet>();
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            var castOrigin = _playerController.Position;

            foreach (var ability in _activeAbilities)
                ability.Tick(Time.deltaTime, castOrigin, _statSheet);
        }

        /// <summary>
        /// Grants a new ability at the given rarity, or upgrades the rarity
        /// of an existing one if the player already owns it — matching how
        /// rarity-tiered roguelike pickups typically work (no duplicate entries).
        /// </summary>
        public void GrantOrUpgradeAbility(string displayName, IAbility ability, IAbilityRarityStats stats, Rarity rarity)
        {
            var existingIndex = _activeAbilities.FindIndex(a => a.DisplayName == displayName);
            if (existingIndex >= 0)
            {
                if (_activeAbilities[existingIndex].Rarity >= rarity)
                {
                    Debug.LogWarning($"PlayerAbilityManager: '{displayName}' already owned at " +
                                      $"{_activeAbilities[existingIndex].Rarity} or higher — ignoring lower-rarity grant.", this);
                    return;
                }

                _activeAbilities.RemoveAt(existingIndex);
            }

            var runtime = new AbilityRuntime(displayName, ability, stats, rarity);
            runtime.InitializeCooldown(_statSheet);
            _activeAbilities.Add(runtime);
        }
    }
}
