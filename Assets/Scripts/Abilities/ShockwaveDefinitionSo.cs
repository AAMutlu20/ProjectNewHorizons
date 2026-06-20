using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Shockwave's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/Shockwave. Default values below
    /// match the design doc exactly — adjust in the Inspector if balance
    /// changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/Shockwave", fileName = "Ability_Shockwave")]
    public class ShockwaveDefinitionSo : AbilityDefinitionSo<ShockwaveStats>
    {
        private void Reset()
        {
            displayName = "Shockwave";
            statsByRarity = new[]
            {
                new ShockwaveStats { Damage = 50f,  Radius = 15f, Cooldown = 5f, StunDuration = 0.2f },
                new ShockwaveStats { Damage = 150f, Radius = 25f, Cooldown = 4f, StunDuration = 0.8f },
                new ShockwaveStats { Damage = 200f, Radius = 30f, Cooldown = 4f, StunDuration = 1.2f },
                new ShockwaveStats { Damage = 300f, Radius = 45f, Cooldown = 4f, StunDuration = 2f },
            };
        }
    }
}
