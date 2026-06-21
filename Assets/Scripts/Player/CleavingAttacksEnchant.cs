using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Cleaving Attacks melee enchant: every AttacksPerTrigger swings, the
    /// triggering swing's hits ALSO deal BonusDamage and apply a bleed DOT,
    /// to every enemy hit in that swing (matching "swings with such strength
    /// that his weapon tears through enemies" — plural, the whole swing's
    /// hits get the bonus, not just one target).
    ///
    /// Buffers hits during OnMeleeHit (we don't yet know if this swing is
    /// the trigger swing — the counter only resolves once the whole swing
    /// is known, in OnSwingComplete) and applies the bonus retroactively
    /// once OnSwingComplete confirms whether this was a trigger swing.
    ///
    /// At Legendary (AttacksPerTrigger = 1), also applies a weaken debuff to
    /// every hit enemy, since every swing is a cleaving swing at that tier.
    ///
    /// Attach to: the same GameObject as MeleeWeapon.
    /// </summary>
    public class CleavingAttacksEnchant : MonoBehaviour, IMeleeEnchant
    {
        [SerializeField] private Abilities.CleavingDefinitionSo definition;

        private Abilities.CleavingStats _stats;
        private bool _isGranted;
        private int _swingsSinceLastTrigger;

        // Buffered for the swing currently in progress — cleared every OnSwingComplete.
        private readonly List<(EnemyView target, Vector3 hitOrigin)> _hitsThisSwing = new();

        private void Awake()
        {
            Debug.Assert(definition, "CleavingAttacksEnchant: definition not assigned.", this);
        }

        public void GrantEnchant(Stats.Rarity rarity)
        {
            _stats = definition.GetStatsForRarity(rarity);
            _isGranted = true;
            _swingsSinceLastTrigger = 0;
        }

        public void OnMeleeHit(EnemyView target, float damageDealt, Vector3 hitOrigin)
        {
            if (!_isGranted) return;
            _hitsThisSwing.Add((target, hitOrigin));
        }

        public void OnSwingComplete(int enemiesHitThisSwing)
        {
            if (!_isGranted)
            {
                _hitsThisSwing.Clear();
                return;
            }

            _swingsSinceLastTrigger++;
            var isTriggerSwing = _swingsSinceLastTrigger >= Mathf.Max(1, _stats.AttacksPerTrigger);

            if (isTriggerSwing)
            {
                ApplyCleaveToBufferedHits();
                _swingsSinceLastTrigger = 0;
            }

            _hitsThisSwing.Clear();
        }

        private void ApplyCleaveToBufferedHits()
        {
            foreach (var (target, hitOrigin) in _hitsThisSwing)
            {
                if (!target || !target.Data.IsAlive) continue;

                target.TakeDamage(_stats.BonusDamage, hitOrigin);
                if (!target.Data.IsAlive) continue; // bonus damage itself may have killed it

                target.DataRef.ApplyBleed(_stats.BleedDamagePercentPerSecond, _stats.BleedDuration);

                if (_stats.WeakenMultiplierBonus > 0f)
                    target.DataRef.ApplyWeaken(1f + _stats.WeakenMultiplierBonus, _stats.BleedDuration);
            }
        }
    }
}
