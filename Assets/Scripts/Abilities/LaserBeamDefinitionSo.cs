using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Laser Beam's designer-editable rarity tiers. Create via right-click
    /// Project → Create → Game/Abilities/LaserBeam.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Abilities/LaserBeam", fileName = "Ability_LaserBeam")]
    public class LaserBeamDefinitionSo : AbilityDefinitionSo<LaserBeamStats>
    {
        private void Reset()
        {
            displayName = "Laser Beam";
            statsByRarity = new[]
            {
                new LaserBeamStats { BeamCount = 1, WeakeningMultiplierBonus = 0.10f, DamagePercentMaxHpPerSecond = 0.012f, Duration = 5f, Cooldown = 13f },
                new LaserBeamStats { BeamCount = 2, WeakeningMultiplierBonus = 0.11f, DamagePercentMaxHpPerSecond = 0.015f, Duration = 6f, Cooldown = 12f },
                new LaserBeamStats { BeamCount = 3, WeakeningMultiplierBonus = 0.12f, DamagePercentMaxHpPerSecond = 0.0175f, Duration = 7f, Cooldown = 11f },
                new LaserBeamStats { BeamCount = 4, WeakeningMultiplierBonus = 0.15f, DamagePercentMaxHpPerSecond = 0.02f, Duration = 8f, Cooldown = 10f },
            };
        }
    }
}
