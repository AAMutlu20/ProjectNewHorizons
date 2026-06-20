using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Dark Shield's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/DarkShield.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/DarkShield", fileName = "Ability_DarkShield")]
    public class DarkShieldDefinitionSo : AbilityDefinitionSo<DarkShieldStats>
    {
        private void Reset()
        {
            displayName = "Dark Shield";
            statsByRarity = new[]
            {
                new DarkShieldStats { Layers = 1, LayerRegenSeconds = 30f, FullShieldRegenSeconds = 60f },
                new DarkShieldStats { Layers = 2, LayerRegenSeconds = 30f, FullShieldRegenSeconds = 60f },
                new DarkShieldStats { Layers = 3, LayerRegenSeconds = 30f, FullShieldRegenSeconds = 60f },
                new DarkShieldStats { Layers = 4, LayerRegenSeconds = 30f, FullShieldRegenSeconds = 60f },
            };
        }
    }
}
