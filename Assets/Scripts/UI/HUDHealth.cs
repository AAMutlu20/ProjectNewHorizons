using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drives a health bar Image fill and a centered HP number label from
    /// PlayerHealthChangedEvent. No reference to PlayerHealth — purely
    /// event driven.
    ///
    /// hpLabel shows the current HP as a whole number (e.g. "205"), centered
    /// inside the bar -- matching the reference layout where the number sits
    /// directly on top of the fill, not as a separate "123 / 200" readout.
    ///
    /// Attach to: HUD_Canvas root. Wire healthBarFill Image and hpLabel Text in Inspector.
    /// </summary>
    public class HUDHealth : MonoBehaviour
    {
        [SerializeField] private Image healthBarFill;
        [SerializeField] private TextMeshProUGUI hpLabel;

        private void Start()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            if (healthBarFill) healthBarFill.fillAmount = 1f;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (healthBarFill)
                healthBarFill.fillAmount = evt.Max > 0f ? evt.Current / evt.Max : 0f;

            if (hpLabel)
                hpLabel.text = Mathf.RoundToInt(evt.Current).ToString();
        }
    }
}
