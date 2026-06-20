namespace Difficulty
{
    /// <summary>
    /// Pure data struct describing how much stronger/more numerous enemies
    /// should be right now. Computed from elapsed game time and the number
    /// of boss kills so far — there is no "wave index" in the new infinite
    /// cycle model, since the game has no fixed end.
    /// </summary>
    public struct DifficultyParams
    {
        public float HpMult;       // multiplier on EnemyTypeSo.baseHp
        public float DamageMult;   // multiplier on EnemyTypeSo.baseDamage
        public float SpawnRadius;  // world-units from arena centre where enemies appear
        public float Budget;       // max enemies active simultaneously (soft cap)
    }
}
