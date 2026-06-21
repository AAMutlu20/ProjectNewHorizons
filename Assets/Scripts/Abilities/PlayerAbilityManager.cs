using System.Collections.Generic;
using Enemies;
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
    /// MaxAbilitySlots defines a FIXED slot count for UI purposes (the
    /// ability diamond row) — empty slots beyond _activeAbilities.Count
    /// should render visibly empty rather than collapsing the layout.
    ///
    /// Attach to: PlayerRoot, alongside StatSheet and PlayerController.
    /// Wire: enemyPool reference in Inspector (the scene's single EnemyPool).
    /// </summary>
    [RequireComponent(typeof(StatSheet))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAbilityManager : MonoBehaviour
    {
        public const int MaxAbilitySlots = 6;

        [SerializeField] private EnemyPool enemyPool;

        private readonly List<AbilityRuntime> _activeAbilities = new();

        private StatSheet _statSheet;
        private PlayerController _playerController;

        public IReadOnlyList<AbilityRuntime> ActiveAbilities => _activeAbilities;

        private void Awake()
        {
            _statSheet = GetComponent<StatSheet>();
            _playerController = GetComponent<PlayerController>();

            Debug.Assert(enemyPool, "PlayerAbilityManager: enemyPool not assigned.", this);
        }

        private void Update()
        {
            var castOrigin = _playerController.Position;

            foreach (var ability in _activeAbilities)
                ability.Tick(Time.deltaTime, castOrigin, _statSheet, enemyPool);
        }

        /// <summary>
        /// Grants a new ability at the given rarity, or upgrades the rarity
        /// of an existing one if the player already owns it — matching how
        /// rarity-tiered roguelike pickups typically work (no duplicate entries).
        /// Silently refuses if all MaxAbilitySlots are already filled with
        /// distinct abilities, since the UI has a fixed number of slots to show them in.
        /// </summary>
        public void GrantOrUpgradeAbility(string displayName, IAbility ability, IAbilityRarityStats stats, Rarity rarity, Sprite icon)
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
            else if (_activeAbilities.Count >= MaxAbilitySlots)
            {
                Debug.LogWarning($"PlayerAbilityManager: all {MaxAbilitySlots} ability slots full — " +
                                  $"cannot grant '{displayName}'.", this);
                return;
            }

            var runtime = new AbilityRuntime(displayName, ability, stats, rarity, icon);
            runtime.InitializeCooldown(_statSheet);
            _activeAbilities.Add(runtime);
        }
    }
}
