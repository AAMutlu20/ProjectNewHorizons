using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drives the full-width bottom XP bar and the level-number diamond
    /// from ExperienceChangedEvent. Shared between desktop and mobile —
    /// both layouts use this same strip-at-the-bottom pattern per the
    /// design reference (Soulstone Survivors desktop, Magic Survival mobile).
    ///
    /// Attach to: HUD canvas, bottom edge.
    /// </summary>
    public class HUDExperience : MonoBehaviour
    {
        [SerializeField] private Image xpBarFill;
        [SerializeField] private TextMeshProUGUI levelLabel;

        private void OnEnable()
        {
            EventBus.Subscribe<ExperienceChangedEvent>(OnExperienceChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ExperienceChangedEvent>(OnExperienceChanged);
        }

        private void OnExperienceChanged(ExperienceChangedEvent experienceChanged)
        {
            if (xpBarFill)
                xpBarFill.fillAmount = experienceChanged.XpToNextLevel > 0f
                    ? experienceChanged.CurrentXp / experienceChanged.XpToNextLevel
                    : 0f;

            if (levelLabel)
                levelLabel.text = experienceChanged.Level.ToString();
        }
    }
}
