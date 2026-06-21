using Stats;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One ability slot in the diamond row. Renders empty (no icon, dimmed
    /// border) until bound to an AbilityRuntime, then shows the icon, a
    /// rarity-coloured border, and a radial cooldown fill that depletes as
    /// the ability gets closer to ready (Image.fillAmount on a Radial360
    /// Image, matching cooldown UIs in games like this).
    ///
    /// Attach to: one diamond-shaped slot in the ability row prefab.
    /// </summary>
    public class AbilitySlotWidget : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;

        [Tooltip("A Radial360-filled Image overlay, drawn on top of the icon, showing remaining cooldown.")]
        [SerializeField] private Image cooldownOverlay;

        [Header("Rarity colours — index order: Common, Rare, Epic, Legendary")]
        [SerializeField] private Color[] rarityColors =
        {
            new(0.75f, 0.75f, 0.75f),
            new(0.25f, 0.55f, 1f),
            new(0.65f, 0.25f, 0.95f),
            new(1f, 0.65f, 0.1f),
        };

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
    }
}
