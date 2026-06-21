using Player;
using Stats;
using UnityEngine;
using XP;

namespace Enemies
{
    /// <summary>
    /// A pooled XP orb dropped on enemy death (see XpOrbPool). Sits in place
    /// until the player comes within PickupRange (a percent bonus on a
    /// 10-unit base, per the design doc), then flies toward the player and
    /// grants its XP value on contact.
    ///
    /// Pooled, not Instantiate/Destroy'd -- matches the project's convention
    /// for anything that can exist in large numbers on screen at once
    /// (projectiles, telegraph rings, DOT zones, and now orbs).
    ///
    /// Attach to: the orb prefab root, with a Collider set to IsTrigger
    /// (used only as a fallback contact check -- the primary pickup trigger
    /// is the PickupRange distance check in Update, not physics overlap).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class XpOrb : MonoBehaviour
    {
        private const float BasePickupRange = 10f; // doc: "Base pickup range is 10 units"
        private const float FlySpeed = 14f;
        private const float PickupDistance = 0.5f; // close enough to grant, once flying toward the player

        [System.NonSerialized] public XpOrbPool Pool;

        private float _xpValue;
        private bool _isActive;
        private bool _isFlyingToPlayer;
        private Transform _playerTransform;
        private StatSheet _playerStatSheet;
        private LevelSystem _levelSystem;

        /// <summary>Activates the orb at worldPosition carrying xpValue. Called by XpOrbPool.Spawn().</summary>
        public void Activate(Vector3 worldPosition, float xpValue, Transform playerTransform,
            StatSheet playerStatSheet, LevelSystem levelSystem)
        {
            transform.position = worldPosition;
            _xpValue = xpValue;
            _playerTransform = playerTransform;
            _playerStatSheet = playerStatSheet;
            _levelSystem = levelSystem;

            _isActive = true;
            _isFlyingToPlayer = false;
        }

        private void Update()
        {
            if (!_isActive) return;

            var distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (!_isFlyingToPlayer)
            {
                var pickupRange = _playerStatSheet.ApplyPercentBonus(BasePickupRange, StatType.PickupRange);
                if (distanceToPlayer <= pickupRange)
                    _isFlyingToPlayer = true;

                return;
            }

            if (distanceToPlayer <= PickupDistance)
            {
                Collect();
                return;
            }

            var direction = (_playerTransform.position - transform.position).normalized;
            transform.position += direction * (FlySpeed * Time.deltaTime);
        }

        private void Collect()
        {
            _isActive = false;
            _levelSystem.GrantPickedUpXp(_xpValue);
            Pool.Return(this);
        }
    }
}
