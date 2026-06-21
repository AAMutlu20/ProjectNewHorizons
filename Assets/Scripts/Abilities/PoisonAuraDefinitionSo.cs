using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Poison Aura's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/PoisonAura.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/PoisonAura", fileName = "Ability_PoisonAura")]
    public class PoisonAuraDefinitionSo : AbilityDefinitionSo<PoisonAuraStats>
    {
        private void Reset()
        {
            displayName = "Poison Aura";
            statsByRarity = new[]
            {
                new PoisonAuraStats { Damage = 50f, DamageFrequencySeconds = 0.5f, Range = 8f,  SlowFraction = 0f },
                new PoisonAuraStats { Damage = 60f, DamageFrequencySeconds = 0.4f, Range = 9f,  SlowFraction = 0f },
                new PoisonAuraStats { Damage = 70f, DamageFrequencySeconds = 0.3f, Range = 10f, SlowFraction = 0f },
                new PoisonAuraStats { Damage = 80f, DamageFrequencySeconds = 0.2f, Range = 12f, SlowFraction = 0.5f },
            };
        }
    }
}
