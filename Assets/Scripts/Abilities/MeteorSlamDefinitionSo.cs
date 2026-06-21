using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Meteor Slam's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/MeteorSlam.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/MeteorSlam", fileName = "Ability_MeteorSlam")]
    public class MeteorSlamDefinitionSo : AbilityDefinitionSo<MeteorSlamStats>
    {
        private void Reset()
        {
            displayName = "Meteor Slam";
            statsByRarity = new[]
            {
                new MeteorSlamStats { Damage = 200f, ImpactRadius = 10f, Cooldown = 15f,
                    LavaPoolDamagePercentPerSecond = 0f,    LavaPoolRange = 0f,  LavaPoolDuration = 0f },
                new MeteorSlamStats { Damage = 300f, ImpactRadius = 10f, Cooldown = 14f,
                    LavaPoolDamagePercentPerSecond = 0f,    LavaPoolRange = 0f,  LavaPoolDuration = 0f },
                new MeteorSlamStats { Damage = 400f, ImpactRadius = 15f, Cooldown = 13f,
                    LavaPoolDamagePercentPerSecond = 0.10f, LavaPoolRange = 12f, LavaPoolDuration = 6f },
                new MeteorSlamStats { Damage = 600f, ImpactRadius = 20f, Cooldown = 12f,
                    LavaPoolDamagePercentPerSecond = 0.10f, LavaPoolRange = 18f, LavaPoolDuration = 6f },
            };
        }
    }
}
