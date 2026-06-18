using System.Collections.Generic;
using Enemies;
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
    /// Attach to: a child GameObject of PlayerRoot (e.g. "MeleeAura"), with a
    /// SphereCollider or CapsuleCollider set to IsTrigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("Swing timing")]
        [Tooltip("Seconds between the start of one swing and the start of the next.")]
        [SerializeField] private float swingInterval = 1f;
        [Tooltip("Seconds the swing is actually 'live' and can deal damage, starting at the top of each interval.")]
        [SerializeField] private float swingActiveDuration = 0.2f;

        [Header("Damage")]
        [SerializeField] private float damage = 5f;

        [Header("Knockback")]
        [SerializeField] private float knockbackForce = 4f;
        [SerializeField] private float knockbackDuration = 0.2f;

        // Countdown to the next swing. Starts at swingInterval so there's no
        // instant free hit the moment the weapon is enabled.
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

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
                Debug.LogWarning("MeleeWeapon: collider should be set to IsTrigger.", this);

            _timeToNextSwing = swingInterval;
        }

        private void Update()
        {
            _timeToNextSwing -= Time.deltaTime;

            if (!_isSwinging && _timeToNextSwing <= 0f)
            {
                // Start a new swing
                _isSwinging = true;
                _hitThisSwing.Clear();

                // Hit everyone already standing in range the instant the swing starts —
                // without this, an enemy that walked in during the idle window and never
                // re-triggers OnTriggerEnter would never get hit at all.
                DamageEveryoneInRange();
            }
            else if (_isSwinging && _timeToNextSwing <= -swingActiveDuration)
            {
                // Swing window closed — go back to idle and reset the cycle
                _isSwinging = false;
                _timeToNextSwing = swingInterval;
            }
        }

        private void DamageEveryoneInRange()
        {
            foreach (var enemy in _enemiesInRange)
            {
                if (enemy == null) continue; // pooled enemy may have been returned/deactivated
                TryHit(enemy);
            }
        }

        private void TryHit(EnemyView enemy)
        {
            if (_hitThisSwing.Contains(enemy)) return;

            enemy.TakeDamage(damage, transform.position, knockbackForce, knockbackDuration);
            _hitThisSwing.Add(enemy);
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
            if (enemy != null) _enemiesInRange.Remove(enemy);
        }
    }
}