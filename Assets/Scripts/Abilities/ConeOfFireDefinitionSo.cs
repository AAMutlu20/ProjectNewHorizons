using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Cone of Fire's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/ConeOfFire.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/ConeOfFire", fileName = "Ability_ConeOfFire")]
    public class ConeOfFireDefinitionSo : AbilityDefinitionSo<ConeOfFireStats>
    {
        private void Reset()
        {
            displayName = "Cone of Fire";
            statsByRarity = new[]
            {
                new ConeOfFireStats { Damage = 20f, Range = 10f, Cooldown = 8f, ResidualDamagePerSecond = 10f, ResidualDuration = 10f },
                new ConeOfFireStats { Damage = 40f, Range = 11f, Cooldown = 7f, ResidualDamagePerSecond = 20f, ResidualDuration = 5f },
                new ConeOfFireStats { Damage = 60f, Range = 12f, Cooldown = 6f, ResidualDamagePerSecond = 40f, ResidualDuration = 4f },
                new ConeOfFireStats { Damage = 80f, Range = 13f, Cooldown = 5f, ResidualDamagePerSecond = 60f, ResidualDuration = 3f },
            };
        }
    }
}
