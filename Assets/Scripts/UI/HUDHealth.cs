using Core;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drives a health bar Image fill from PlayerHealthChangedEvent.
    /// No reference to PlayerHealth — purely event driven.
    ///
    /// Attach to: HUD_Canvas root. Wire healthBarFill Image in Inspector.
    /// </summary>
    public class HUDHealth : MonoBehaviour
    {
        [SerializeField] private Image healthBarFill;

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
            if (!healthBarFill) return;
            healthBarFill.fillAmount = evt.Max > 0f ? evt.Current / evt.Max : 0f;
        }
    }
}
