using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// SpiderMinion attack: bites the player the instant its trigger collider
    /// overlaps the player — same proven pattern as ZombieExplodeBehaviour,
    /// just without the self-destruct. The minion bites on cooldown and keeps
    /// chasing rather than dying on contact.
    ///
    /// Unlike the zombie, the minion survives the bite — it resets its cooldown
    /// and continues chasing. _hasBit acts as a per-hit cooldown flag, cleared
    /// by ResetBite() on a timer so the minion can bite again.
    ///
    /// Attach to: a CHILD GameObject on the SpiderMinion prefab (e.g. "BiteTrigger"),
    /// with a SphereCollider set to IsTrigger, sized to bite range.
    /// Set the child's layer to match the player body layer so it only fires
    /// on the player, not on other enemies or the melee aura.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpiderMinionBiteBehaviour : MonoBehaviour
    {
        [SerializeField] private LayerMask playerBodyLayers;
        [SerializeField] private float biteCooldown = 1f;

        [System.NonSerialized] public EnemyView OwnerView;

        private bool _hasBit;

        public void ResetForReuse(EnemyView ownerView)
        {
            OwnerView = ownerView;
            _hasBit = false;
            CancelInvoke(nameof(ResetBite));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasBit) return;
            if (!OwnerView || !OwnerView.Data.IsAlive) return;
            if (OwnerView.Data.IsSpawning) return;
            if (playerBodyLayers != 0 && (playerBodyLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            var playerHealth = other.GetComponentInParent<Player.PlayerHealth>();
            if (!playerHealth) return;

            Bite();
        }

        private void Bite()
        {
            _hasBit = true;

            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = OwnerView.Data.Damage,
                Position = OwnerView.Data.Position
            });

            // Reset bite flag after cooldown so the minion can bite again
            Invoke(nameof(ResetBite), biteCooldown);
        }

        private void ResetBite() => _hasBit = false;
    }
}
