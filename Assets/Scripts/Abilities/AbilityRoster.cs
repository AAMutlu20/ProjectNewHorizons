using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// The roster of every ability eligible to be offered. Unlike StatPoolSo
    /// (which lists ScriptableObject assets), this is a plain serializable
    /// list since IAbilityChoiceEntry implementations are scene MonoBehaviours
    /// (they need scene-specific references like PlayerAbilityManager,
    /// DarkShieldController, etc.) -- not assets that exist independent of
    /// a scene.
    ///
    /// Attach to: the same GameObject as AbilityChoiceGenerator, or any
    /// [Systems] object -- wire every IAbilityChoiceEntry in the scene here.
    /// </summary>
    public class AbilityRoster : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] abilityEntries;

        /// <summary>
        /// Cached cast results -- abilityEntries is serialized as
        /// MonoBehaviour (Unity can't serialize interface-typed array fields
        /// directly), so each is cast to IAbilityChoiceEntry once here rather
        /// than on every roll.
        /// </summary>
        public IAbilityChoiceEntry[] GetEntries()
        {
            var entries = new IAbilityChoiceEntry[abilityEntries.Length];
            for (var i = 0; i < abilityEntries.Length; i++)
            {
                entries[i] = abilityEntries[i] as IAbilityChoiceEntry;
                if (entries[i] == null)
                    Debug.LogError($"AbilityRoster: '{abilityEntries[i].name}' does not implement IAbilityChoiceEntry.", this);
            }
            return entries;
        }
    }
}
