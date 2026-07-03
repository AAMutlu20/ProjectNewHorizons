using Core;
using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Zombie attack: explodes the instant its OWN trigger collider actually
    /// overlaps the player's body -- no manual distance/range check at all.
    /// This replaces the old TickAttack/attackRange-gated approach, which
    /// turned out unreliable (every theoretical distance/Y-offset/collider-
    /// radius explanation failed to predict the real symptom) -- a real
    /// physics trigger, the same proven pattern MeleeWeapon already uses for
    /// hitting enemies, is simpler and just works, since Unity's own physics
    /// engine resolves "is this collider actually overlapping that one"
    /// correctly without any hand-rolled math.
    ///
    /// NO LONGER implements IEnemyAttackBehaviour -- the zombie never enters
    /// BehaviourController's attackRange-gated TickAttack path at all now.
    /// It still walks toward the player via BehaviourController's normal
    /// movement (that's unrelated and untouched); only the ATTACK itself
    /// moved to this trigger-based approach.
    ///
    /// Attach to: a SEPARATE child GameObject under the Zombie prefab (e.g.
    /// "ExplosionTrigger"), NOT the Zombie root -- needs its own SphereCollider
    /// (IsTrigger checked) sized to the zombie's explosion radius, distinct
    /// from the zombie's own solid body capsule collider. Set this child's
    /// Layer to whatever layer the PLAYER'S BODY COLLIDER uses (check the
    /// Layer Collision Matrix / the hit collider's layer in code, same
    /// precision fix as the earlier melee-aura-vs-projectile bug) so this
    /// trigger only fires on the player's actual body, never on the melee
    /// aura or any other future child collider under PlayerRoot.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZombieExplodeBehaviour : MonoBehaviour
    {
        [Tooltip("The player's actual body collider must be on one of these layers for this trigger to react to it. " +
                 "Leave at None/0 to accept any collider with a PlayerHealth in its parent chain (less precise, " +
                 "but works if you haven't set up a dedicated player-body layer yet).")]
        [SerializeField] private LayerMask playerBodyLayers;

        // Injected by whatever spawns/pools this zombie (mirrors EnemyView's
        // own injected-reference pattern) -- set this from the Zombie root's
        // EnemyView/BehaviourController once at spawn, since this trigger is
        // a separate child GameObject and needs its own way to reach the
        // zombie's EnemyView for damage/death.
        [System.NonSerialized] public EnemyView OwnerView;

        private bool _hasExploded;

        /// <summary>Call when this zombie is reused from the pool -- clears the one-shot explosion flag and re-arms OwnerView.</summary>
        public void ResetForReuse(EnemyView ownerView)
        {
            OwnerView = ownerView;
            _hasExploded = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasExploded) return;
            if (!OwnerView || !OwnerView.Data.IsAlive) return;

            // Don't explode during the spawn grace period — the zombie just activated
            // and may be physically adjacent to the player or a freshly-spawned minion.
            // TakeDamage also blocks damage during Spawning, but refusing to explode here
            // as well prevents the one-shot _hasExploded flag from being consumed during
            // a spawn-frame overlap that would otherwise permanently disarm the trigger.
            if (OwnerView.Data.IsSpawning) return;

            if (playerBodyLayers != 0 && (playerBodyLayers.value & (1 << other.gameObject.layer)) == 0)
                return; // hit something, but not on a layer we care about
            var playerHealth = other.GetComponentInParent<Player.PlayerHealth>();
            if (!playerHealth) return;
            Explode();
        }

        private void Explode()
        {
            _hasExploded = true;

            var enemy = OwnerView.Data;

            EventBus.Emit(new EnemyAttackEvent
            {
                Damage = enemy.Damage,
                Position = enemy.Position,
            });

            EventBus.Emit(new ZombieExplodedEvent
            {
                Position = enemy.Position,
            });

            OwnerView.TakeDamage(enemy.Hp);
        }
    }

    /// <summary>Emitted when a zombie explodes, for VFX/audio systems to react to.</summary>
    public struct ZombieExplodedEvent
    {
        public Vector3 Position;
    }
}
