using Core;
using Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Desktop boss bar: name + HP fill driven by BossHealthChangedEvent
    /// (event-driven, cheap), plus a row of debuff icons polled directly
    /// from the boss's EnemyView each frame (debuff timers decay every
    /// frame regardless of damage events, so polling is correct here rather
    /// than trying to event-drive something that changes continuously).
    ///
    /// Hidden by default, shown on first BossHealthChangedEvent, hidden
    /// again on the boss's EnemyDiedEvent.
    ///
    /// Attach to: Desktop HUD canvas, top-center. Wire debuffIconPrefab to
    /// a small Image; one is shown per active debuff, toggled rather than
    /// instantiated/destroyed since the debuff set is small and fixed (5).
    /// </summary>
    public class HUDBossBar : MonoBehaviour
    {
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private TextMeshProUGUI bossNameLabel;
        [SerializeField] private Image bossBarFill;
        [SerializeField] private EnemyPool enemyPool;

        [Header("Debuff icons — index order: Stunned, Buffed, Weakened, Slowed, Bleeding")]
        [SerializeField] private Image[] debuffIcons = new Image[5];

        private EnemyView _trackedBoss;

        private void Awake()
        {
            Debug.Assert(enemyPool, "HUDBossBar: enemyPool not assigned.", this);
            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void Update()
        {
            if (!_trackedBoss) return;
            UpdateDebuffIcons(_trackedBoss.Data);
        }

        private void OnBossHealthChanged(BossHealthChangedEvent healthChanged)
        {
            SetVisible(true);
            FindTrackedBossIfNeeded();

            if (bossBarFill)
                bossBarFill.fillAmount = healthChanged.Max > 0f ? healthChanged.Current / healthChanged.Max : 0f;
        }

        private void OnEnemyDied(EnemyDiedEvent enemyDied)
        {
            if (!enemyDied.IsBoss) return;
            _trackedBoss = null;
            SetVisible(false);
        }

        private void FindTrackedBossIfNeeded()
        {
            if (_trackedBoss) return;

            foreach (var enemyView in enemyPool.ActiveEnemies)
            {
                if (enemyView && enemyView.Type == EnemyType.Boss)
                {
                    _trackedBoss = enemyView;
                    if (bossNameLabel) bossNameLabel.text = "Boss"; // no per-boss display name exists yet
                    return;
                }
            }
        }

        private void UpdateDebuffIcons(EnemyData data)
        {
            SetIconActive(0, data.IsStunned);
            SetIconActive(1, data.IsBuffed);
            SetIconActive(2, data.IsWeakened);
            SetIconActive(3, data.IsSlowed);
            SetIconActive(4, data.IsBleeding);
        }

        private void SetIconActive(int index, bool isActive)
        {
            if (index < 0 || index >= debuffIcons.Length || !debuffIcons[index]) return;
            debuffIcons[index].gameObject.SetActive(isActive);
        }

        private void SetVisible(bool isVisible)
        {
            if (bossBarRoot) bossBarRoot.SetActive(isVisible);
        }
    }
}
