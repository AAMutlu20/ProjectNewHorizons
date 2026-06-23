using Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One ability slot in the diamond row. Renders empty (no icon, dimmed
    /// border) until bound to an AbilityRuntime, then shows the icon, a
    /// rarity-coloured border, a radial cooldown fill that depletes as the
    /// ability gets closer to ready, and the rarity's name as text beneath
    /// the slot (e.g. "Rare") -- per design intent, the border colour AND
    /// this label both communicate rarity, reinforcing each other rather
    /// than being the only way to tell (useful for colourblind accessibility
    /// too, though that wasn't the original reason it was requested).
    ///
    /// Attach to: one diamond-shaped slot in the ability row prefab.
    /// </summary>
    public class AbilitySlotWidget : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI rarityLabel;

        [Tooltip("A Radial360-filled Image overlay, drawn on top of the icon, showing remaining cooldown.")]
        [SerializeField] private Image cooldownOverlay;

        [Header("Rarity colours — index order: Common, Rare, Epic, Legendary")]
        [SerializeField] private Color[] rarityColors =
        {
            new(0.75f, 0.75f, 0.75f), // Common - grey
            new(0.25f, 0.55f, 1f),    // Rare - blue
            new(0.65f, 0.25f, 0.95f), // Epic - purple
            new(1f, 0.65f, 0.1f),     // Legendary - orange
        };

        private static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary" };

        [SerializeField] private Color emptySlotBorderColor = new(1f, 1f, 1f, 0.2f);

        private Abilities.AbilityRuntime _boundAbility;

        private void Awake()
        {
            SetEmpty();
        }

        private void Update()
        {
            if (_boundAbility == null) return;
            if (cooldownOverlay) cooldownOverlay.fillAmount = 1f - _boundAbility.CooldownProgress01;
        }

        public void Bind(Abilities.AbilityRuntime ability)
        {
            _boundAbility = ability;

            if (iconImage)
            {
                iconImage.sprite = ability.Icon;
                iconImage.enabled = ability.Icon != null;
            }

            if (rarityBorder) rarityBorder.color = GetRarityColor(ability.Rarity);
            if (cooldownOverlay) cooldownOverlay.enabled = true;

            if (rarityLabel)
            {
                rarityLabel.text = GetRarityName(ability.Rarity);
                rarityLabel.enabled = true;
            }
        }

        public void SetEmpty()
        {
            _boundAbility = null;

            if (iconImage) iconImage.enabled = false;
            if (rarityBorder) rarityBorder.color = emptySlotBorderColor;
            if (cooldownOverlay)
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownOverlay.enabled = false;
            }

            if (rarityLabel) rarityLabel.enabled = false;
        }

        private Color GetRarityColor(Rarity rarity)
        {
            var index = (int)rarity;
            if (index < 0 || index >= rarityColors.Length)
            {
                Debug.LogWarning($"AbilitySlotWidget: no colour configured for rarity {rarity}.", this);
                return Color.white;
            }
            return rarityColors[index];
        }

        private string GetRarityName(Rarity rarity)
        {
            var index = (int)rarity;
            if (index < 0 || index >= RarityNames.Length)
            {
                Debug.LogWarning($"AbilitySlotWidget: no name configured for rarity {rarity}.", this);
                return rarity.ToString();
            }
            return RarityNames[index];
        }
    }
}
