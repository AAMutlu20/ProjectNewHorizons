using System.Collections.Generic;
using Enemies;
using Stats;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Swing-style melee weapon — a trigger volume around the player that only deals
    /// damage during a brief "active" window on a repeating cycle, not continuously
    /// while an enemy overlaps it. Think "sword swings every second," not "aura that
    /// always hurts you while you stand in it."
    ///
    /// Cycle: idle for (swingInterval - swingActiveDuration), then active for
    /// swingActiveDuration, then repeat. Each enemy can only be hit once per swing,
    /// even if they stay inside the trigger for the whole active window.
    ///
    /// Damage, swing speed, and critical strikes are sourced from StatSheet —
    /// see the design doc's Attack Damage / Attack Speed / Critical Strike stats.
    ///
    /// Melee enchants (Cleaving Attacks, Lifesteal, Knockback) are discovered as
    /// sibling IMeleeEnchant components and called automatically — see IMeleeEnchant.
    ///
    /// Attach to: a child GameObject of PlayerRoot (e.g. "MeleeAura"), with a
    /// SphereCollider or CapsuleCollider set to IsTrigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MeleeWeapon : MonoBehaviour
    {
        // Per the design doc: a critical strike deals 200% of normal damage by
        // default, before the Critical Strike Damage stat adds on top of that.
        private const float BaseCriticalStrikeMultiplier = 200f;
        private const float PercentToFraction = 100f;

        [Header("Swing timing")]
        [Tooltip("Seconds between the start of one swing and the start of the next, before Attack Speed.")]
        [SerializeField] private float baseSwingInterval = 1f;
        [Tooltip("Seconds the swing is actually 'live' and can deal damage, starting at the top of each interval.")]
        [SerializeField] private float swingActiveDuration = 0.2f;

        [Header("Damage")]
        [SerializeField] private float baseDamage = 5f;

        [Header("Knockback")]
        [SerializeField] private float baseKnockbackForce = 4f;
        [SerializeField] private float knockbackDuration = 0.2f;

        private StatSheet _statSheet;
        private IMeleeEnchant[] _enchants;

        // Countdown to the next swing. Starts at baseSwingInterval so there's
        // no instant free hit the moment the weapon is enabled.
        private float _timeToNextSwing;

        // True only during the swingActiveDuration window at the top of each cycle.
        private bool _isSwinging;

        // Enemies already hit during the CURRENT swing — cleared every time a new
        // swing starts, so the same enemy can be hit again next swing but never
        // twice within one swing even if they sit inside the trigger the whole time.
        private readonly HashSet<EnemyView> _hitThisSwing = new();

        // Reused list so OnTriggerStay doesn't need to re-query anything — colliders
        // currently inside the trigger are tracked via enter/exit instead of polled.
        private readonly List<EnemyView> _enemiesInRange = new();

        private float CurrentSwingInterval =>
            baseSwingInterval / (1f + _statSheet.GetTotal(StatType.AttackSpeed) / PercentToFraction);

        private float CurrentKnockbackForce()
        {
            // If a Knockback enchant is granted, it defines the TOTAL force for
            // that tier (per the doc's KnockbackIs values reading as totals, not
            // additive bonuses) — it overrides baseKnockbackForce rather than
            // adding to it. Multiple enchants overriding would be a misconfiguration;
            // the last one found wins, since only one Knockback enchant should
            // ever be attached.
            foreach (var enchant in _enchants)
            {
                var overrideForce = enchant.GetKnockbackForceOverride();
                if (overrideForce.HasValue)
                    return overrideForce.Value;
            }

            return baseKnockbackForce;
        }

        private void Awake()
        {
            _statSheet = GetComponentInParent<StatSheet>();
            _enchants = GetComponents<IMeleeEnchant>();

            var col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning("MeleeWeapon: collider should be set to IsTrigger.", this);

            _timeToNextSwing = baseSwingInterval;
        }

        private void Update()
        {
            _timeToNextSwing -= Time.deltaTime;

            switch (_isSwinging)
            {
                case false when _timeToNextSwing <= 0f:
                    StartSwing();
                    break;
                case true when _timeToNextSwing <= -swingActiveDuration:
                    EndSwing();
                    break;
            }
        }

        private void StartSwing()
        {
            _isSwinging = true;
            _hitThisSwing.Clear();

            // Hit everyone already standing in range the instant the swing starts —
            // without this, an enemy that walked in during the idle window and never
            // re-triggers OnTriggerEnter would never get hit at all.
            DamageEveryoneInRange();
        }

        private void EndSwing()
        {
            _isSwinging = false;
            _timeToNextSwing = CurrentSwingInterval;

            foreach (var enchant in _enchants)
                enchant.OnSwingComplete(_hitThisSwing.Count);
        }

        private void DamageEveryoneInRange()
        {
            foreach (var enemy in _enemiesInRange)
            {
                if (!enemy) continue; // pooled enemy may have been returned/deactivated
                TryHit(enemy);
            }
        }

        private void TryHit(EnemyView enemy)
        {
            if (_hitThisSwing.Contains(enemy)) return;

            var damage = RollDamage();
            enemy.TakeDamage(damage, transform.position, CurrentKnockbackForce(), knockbackDuration);
            _hitThisSwing.Add(enemy);

            foreach (var enchant in _enchants)
                enchant.OnMeleeHit(enemy, damage, transform.position);
        }

        private float RollDamage()
        {
            var totalDamage = baseDamage + _statSheet.GetTotal(StatType.AttackDamage);

            var critChancePercent = _statSheet.GetTotal(StatType.CriticalStrikeChance);
            var rolledCrit = Random.Range(0f, PercentToFraction) < critChancePercent;
            if (!rolledCrit) return totalDamage;

            var critMultiplierPercent = BaseCriticalStrikeMultiplier + _statSheet.GetTotal(StatType.CriticalStrikeDamage);
            return totalDamage * (critMultiplierPercent / PercentToFraction);
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponentInParent<EnemyView>();
            if (!enemy) return;

            if (!_enemiesInRange.Contains(enemy))
                _enemiesInRange.Add(enemy);

            // If a swing is already active when the enemy walks in, hit them immediately
            // instead of making them wait for the next cycle.
            if (_isSwinging)
                TryHit(enemy);
        }

        private void OnTriggerExit(Collider other)
        {
            var enemy = other.GetComponentInParent<EnemyView>();
            if (enemy) _enemiesInRange.Remove(enemy);
        }
    }
}
