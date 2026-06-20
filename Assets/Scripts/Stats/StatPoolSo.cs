using UnityEngine;

namespace Stats
{
    /// <summary>
    /// The full set of stat definitions eligible to appear in level-up and
    /// boss-reward choices. Kept separate from StatDefinitionSo itself so
    /// the pool can be edited (e.g. removing a stat from rotation) without
    /// touching individual stat assets.
    ///
    /// Create via: right-click Project → Create → Game/Stats/StatPool
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Stats/StatPool", fileName = "StatPool_Default")]
    public class StatPoolSo : ScriptableObject
    {
        public StatDefinitionSo[] availableStats;
    }
}
