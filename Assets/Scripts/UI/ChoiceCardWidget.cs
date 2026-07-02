using Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One choice card — renders a ChoiceCardData and invokes its OnSelected
    /// callback when clicked. Knows nothing about stats, abilities, or any
    /// other concrete choice type; purely a display + click adapter.
    ///
    /// Adding a new choice category later never touches this file — it just
    /// needs to be converted into a ChoiceCardData somewhere upstream.
    ///
    /// Attach to: the card prefab root, with a Button component.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ChoiceCardWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI descriptionLabel;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;

        [SerializeField] private bool useRaritySprites;

        [Header("Rarity colours — index order: Common, Rare, Epic, Legendary")]
        [SerializeField] private Color[] rarityColors =
        {
            new(0.75f, 0.75f, 0.75f), // Common - grey
            new(0.25f, 0.55f, 1f),    // Rare - blue
            new(0.65f, 0.25f, 0.95f), // Epic - purple
            new(1f, 0.65f, 0.1f),     // Legendary - orange/gold
        };
        [SerializeField] private Sprite[] RaritySprites;


        private System.Action _onSelected;

        private void Awake()
        {
            if (!selectButton) selectButton = GetComponent<Button>();
            selectButton.onClick.AddListener(HandleClicked);
        }

        /// <summary>Populates this card's display and click handler from data. Call once per card per screen open.</summary>
        public void Bind(ChoiceCardData data)
        {
            if (titleLabel) titleLabel.text = data.Title;
            if (descriptionLabel) descriptionLabel.text = data.Description;
            if (iconImage) iconImage.sprite = data.Icon;

            if(useRaritySprites)
            {
                if (rarityBorder) rarityBorder.sprite = GetRaritySprite(data.Rarity);
            }
            if (rarityBorder) rarityBorder.color = GetRarityColor(data.Rarity);
            

            _onSelected = data.OnSelected;
        }

        private Color GetRarityColor(Rarity rarity)
        {
            var index = (int)rarity;
            if (index < 0 || index >= rarityColors.Length)
            {
                Debug.LogWarning($"ChoiceCardWidget: no colour configured for rarity {rarity}.", this);
                return Color.white;
            }
            return rarityColors[index];
        }

        private Sprite GetRaritySprite(Rarity rarity)
        {
            var index = (int)rarity;
            if (index < 0 || index >= RaritySprites.Length)
            {
                Debug.LogWarning($"ChoiceCardWidget: no sprite configured for rarity {rarity}.", this);
                return null;
            }
            return RaritySprites[index];
        }

        private void HandleClicked()
        {
            _onSelected?.Invoke();
        }
    }
}
