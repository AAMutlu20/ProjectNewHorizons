using Core;
using Enemies;
using Stats;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// One ability the player currently owns: which ability, at what rarity,
    /// counting down to its next automatic cast. Cooldown is scaled by the
    /// player's Ability Haste stat via HasteFormula, recalculated each time
    /// the cooldown resets so haste picked up mid-run applies immediately.
    ///
    /// Emits AbilityCastEvent every time Cast fires, here rather than inside
    /// each IAbility implementation — so every ability gets a VFX hook point
    /// automatically, without each one needing to remember to emit it.
    /// </summary>
    public class AbilityRuntime
    {
        private readonly IAbility _ability;
        private readonly IAbilityRarityStats _stats;
        private readonly Rarity _rarity;

        private float _cooldownTimer;

        public string DisplayName { get; }
        public Rarity Rarity => _rarity;

        public AbilityRuntime(string displayName, IAbility ability, IAbilityRarityStats stats, Rarity rarity)
        {
            DisplayName = displayName;
            _ability = ability;
            _stats = stats;
            _rarity = rarity;
        }

        /// <summary>Advances the cooldown and casts automatically when it elapses. Call once per frame.</summary>
        public void Tick(float deltaTime, Vector3 castOrigin, StatSheet statSheet, EnemyPool enemyPool)
        {
            _cooldownTimer -= deltaTime;
            if (_cooldownTimer > 0f) return;

            _ability.Cast(castOrigin, _rarity, statSheet, enemyPool);

            EventBus.Emit(new AbilityCastEvent
            {
                AbilityName = DisplayName,
                Rarity = _rarity,
                CastOrigin = castOrigin,
            });

            ResetCooldown(statSheet);
        }

        /// <summary>Call once when the ability is first granted so it doesn't cast instantly on pickup.</summary>
        public void InitializeCooldown(StatSheet statSheet)
        {
            ResetCooldown(statSheet);
        }

        private void ResetCooldown(StatSheet statSheet)
        {
            var haste = statSheet.GetTotal(StatType.AbilityHaste);
            _cooldownTimer = HasteFormula.GetCooldown(_stats.Cooldown, haste);
        }
    }

    /// <summary>
    /// Emitted every time any ability casts. Carries only generic info
    /// (name, rarity, origin) — ability-specific VFX needs (e.g. Shockwave's
    /// radius) come from that ability's own definition asset, which the VFX
    /// listener should hold its own reference to rather than this event
    /// trying to carry every possible ability's specific parameters.
    /// </summary>
    public struct AbilityCastEvent
    {
        public string AbilityName;
        public Rarity Rarity;
        public Vector3 CastOrigin;
    }
}
