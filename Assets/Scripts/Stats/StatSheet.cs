using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Stats
{
    /// <summary>
    /// Runtime total of every stat the player has collected. Other systems
    /// (PlayerHealth, MeleeWeapon, abilities) read GetTotal(StatType) instead
    /// of holding their own copies of stat math.
    ///
    /// Attach to: PlayerRoot.
    /// </summary>
    public class StatSheet : MonoBehaviour
    {
        private readonly Dictionary<StatType, float> _totalByStat = new();
        private readonly List<StatModifier> _collectedModifiers = new();

        public IReadOnlyList<StatModifier> CollectedModifiers => _collectedModifiers;

        public float GetTotal(StatType statType)
        {
            return _totalByStat.TryGetValue(statType, out var total) ? total : 0f;
        }

        /// <summary>
        /// Applies a percent-additive stat as a multiplier on a base value.
        /// Example: base damage 50 with 14% Ability Power total returns 57.
        /// </summary>
        public float ApplyPercentBonus(float baseValue, StatType statType)
        {
            const float percentToFraction = 100f;
            return baseValue * (1f + GetTotal(statType) / percentToFraction);
        }

        public void ApplyModifier(StatModifier modifier)
        {
            var statType = modifier.Definition.statType;
            var currentTotal = GetTotal(statType);

            _totalByStat[statType] = currentTotal + modifier.Value;
            _collectedModifiers.Add(modifier);

            EventBus.Emit(new StatCollectedEvent
            {
                StatType = statType,
                Rarity = modifier.Rarity,
                NewTotal = _totalByStat[statType],
            });
        }
    }
}
