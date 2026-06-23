using Core;
using Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Desktop boss bar: name + HP fill + HP number (event-driven, cheap),
    /// plus a dynamic debuff icon row polled directly from the boss's
    /// EnemyView each frame (debuff timers decay every frame regardless of
    /// damage events, so polling is correct here rather than event-driving
    /// something that changes continuously).
    ///
    /// Per design: icons only appear once that debuff type is actually
    /// active (no fixed empty slots), and the row visually grows outward
    /// from CENTER as more debuff types become active -- achieved by
    /// parenting all icons under one HorizontalLayoutGroup with
    /// Child Alignment = Middle Center, NOT by any manual position math
    /// here; this script only ever shows/hides/binds icons, Unity's layout
    /// system handles the centering automatically as children are
    /// activated/deactivated.
    ///
    /// "Buffed" is intentionally NOT shown here -- a buff is the enemy
    /// receiving help from another enemy (e.g. Eye's pulse), not something
    /// the player inflicted, so it has no place in a player-facing debuff
    /// display. Debuffs shown: Stunned, Weakened, Slowed, Bleeding, Burning.
    ///
    /// Hidden by default, shown on first BossHealthChangedEvent, hidden
    /// again on the boss's EnemyDiedEvent.
    ///
    /// Attach to: Desktop HUD canvas, top-center. The 5 DebuffIconWidget
    /// fields below are pre-placed instances inside one HorizontalLayoutGroup
    /// (NOT instantiated at runtime, since the debuff set is small and
    /// fixed) -- each just activates/deactivates itself via Bind/Hide.
    /// </summary>
    public class HUDBossBar : MonoBehaviour
    {
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private TextMeshProUGUI bossNameLabel;
        [SerializeField] private Image bossBarFill;
        [SerializeField] private TextMeshProUGUI hpLabel;
        [SerializeField] private EnemyPool enemyPool;

        [Header("Debuff icons -- order: Stunned, Weakened, Slowed, Bleeding, Burning")]
        [SerializeField] private DebuffIconWidget stunnedIcon;
        [SerializeField] private DebuffIconWidget weakenedIcon;
        [SerializeField] private DebuffIconWidget slowedIcon;
        [SerializeField] private DebuffIconWidget bleedingIcon;
        [SerializeField] private DebuffIconWidget burningIcon;

        [Header("Debuff icon sprites")]
        [SerializeField] private Sprite stunnedSprite;
        [SerializeField] private Sprite weakenedSprite;
        [SerializeField] private Sprite slowedSprite;
        [SerializeField] private Sprite bleedingSprite;
        [SerializeField] private Sprite burningSprite;

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

            if (hpLabel)
                hpLabel.text = Mathf.RoundToInt(healthChanged.Current).ToString("N0");
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
            // Stun never stacks -- BindNoStackCount, radial only.
            if (data.IsStunned) stunnedIcon.BindNoStackCount(stunnedSprite, data.StunRemainingFraction01);
            else stunnedIcon.Hide();

            if (data.IsWeakened) weakenedIcon.Bind(weakenedSprite, data.WeakenStack.RemainingFraction01, data.WeakenStack.StackCount);
            else weakenedIcon.Hide();

            if (data.IsSlowed) slowedIcon.Bind(slowedSprite, data.SlowStack.RemainingFraction01, data.SlowStack.StackCount);
            else slowedIcon.Hide();

            if (data.IsBleeding) bleedingIcon.Bind(bleedingSprite, data.BleedStack.RemainingFraction01, data.BleedStack.StackCount);
            else bleedingIcon.Hide();

            if (data.IsBurning) burningIcon.Bind(burningSprite, data.BurnStack.RemainingFraction01, data.BurnStack.StackCount);
            else burningIcon.Hide();
        }

        private void SetVisible(bool isVisible)
        {
            if (bossBarRoot) bossBarRoot.SetActive(isVisible);
        }
    }
}
