using Core;
using Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Drives the boss bar Image fill from BossHealthChangedEvent. Hidden by
    /// default and shown the moment a boss spawns (first BossHealthChangedEvent),
    /// hidden again when the boss dies (EnemyDiedEvent with IsBoss).
    ///
    /// Attach to: HUD_Canvas root. Wire bossBarFill Image and bossBarRoot in Inspector.
    /// </summary>
    public class HUDBossBar : MonoBehaviour
    {
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossBarFill;

        private void Start()
        {
            EventBus.Subscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);

            SetVisible(false);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnBossHealthChanged(BossHealthChangedEvent healthChanged)
        {
            SetVisible(true);

            if (!bossBarFill) return;
            bossBarFill.fillAmount = healthChanged.Max > 0f
                ? healthChanged.Current / healthChanged.Max
                : 0f;
        }

        private void OnEnemyDied(EnemyDiedEvent enemyDied)
        {
            if (enemyDied.IsBoss) SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (bossBarRoot) bossBarRoot.SetActive(isVisible);
        }
    }
}
