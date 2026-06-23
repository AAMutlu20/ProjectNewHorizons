using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// One debuff icon: a fixed background icon image, a radial-360 fill
    /// overlay counting down the shared debuff timer, and a stack-count
    /// number label -- matching the reference screenshots exactly (the
    /// green circle is the radial countdown, the number is the stack count).
    ///
    /// Hidden entirely when not bound to an active debuff. Stun never shows
    /// a stack count (it doesn't stack) -- pass stackCount = 0 or call
    /// BindNoStackCount for it.
    ///
    /// Attach to: one debuff icon prefab instance, managed by HUDBossBar's
    /// dynamic row (see that script for why these are spawned/positioned at
    /// runtime rather than being fixed Inspector slots).
    /// </summary>
    public class DebuffIconWidget : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image radialCountdownOverlay; // Image Type: Filled, Fill Method: Radial 360
        [SerializeField] private TextMeshProUGUI stackCountLabel;

        public void Bind(Sprite icon, float remainingFraction01, int stackCount)
        {
            gameObject.SetActive(true);

            if (iconImage) iconImage.sprite = icon;

            // Radial fill shows time REMAINING, counting down from full as the
            // debuff approaches expiry -- matches the reference's draining
            // green ring, not a "filling up" cooldown-style indicator.
            if (radialCountdownOverlay) radialCountdownOverlay.fillAmount = remainingFraction01;

            if (stackCountLabel)
            {
                var showStackCount = stackCount > 1;
                stackCountLabel.gameObject.SetActive(showStackCount);
                if (showStackCount) stackCountLabel.text = stackCount.ToString();
            }
        }

        /// <summary>For Stun -- never shows a stack count since it doesn't stack, only the radial countdown.</summary>
        public void BindNoStackCount(Sprite icon, float remainingFraction01) => Bind(icon, remainingFraction01, stackCount: 0);

        public void Hide() => gameObject.SetActive(false);
    }
}
