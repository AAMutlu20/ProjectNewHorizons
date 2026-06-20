using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Bridges Unity's Animation Event system to gameplay code. Animation
    /// Events can only call methods on components attached to the same
    /// GameObject as the Animator, so this sits there and forwards to
    /// whichever IEnemyAttackBehaviour is on the enemy root.
    ///
    /// Setup once art exists: in the attack clip's Animation window, add an
    /// event at the desired frame (e.g. the swing's contact frame) and set
    /// its function to OnAnimationEvent, with the string parameter matching
    /// whatever the attack behaviour checks for in OnAttackAnimationEvent.
    ///
    /// Attach to: the same GameObject as the Animator component (may be the
    /// enemy root, or a child if the Animator lives on a model child).
    /// </summary>
    public class EnemyAnimationEventReceiver : MonoBehaviour
    {
        private IEnemyAttackBehaviour _attackBehaviour;

        private void Awake()
        {
            // Searches the whole hierarchy since the Animator (and therefore
            // this receiver) may live on a child model object, while the
            // attack behaviour lives on the enemy root alongside BehaviourController.
            _attackBehaviour = GetComponentInParent<IEnemyAttackBehaviour>();

            if (_attackBehaviour == null)
                Debug.LogWarning($"EnemyAnimationEventReceiver on '{name}' found no " +
                                  "IEnemyAttackBehaviour in its parent hierarchy — animation " +
                                  "events will be silently ignored.", this);
        }

        /// <summary>Called directly by an Animation Event authored in the clip editor.</summary>
        public void OnAnimationEvent(string eventName)
        {
            _attackBehaviour?.OnAttackAnimationEvent(eventName);
        }
    }
}
