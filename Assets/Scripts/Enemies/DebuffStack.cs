using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Shared stacking model for player-inflicted debuffs (Weaken, Slow,
    /// Bleed, Burn) -- NOT used for Stun (explicitly non-stacking, duration
    /// only) or Buff (not a debuff at all -- that's the enemy receiving a
    /// buff from another enemy, e.g. Eye's pulse, nothing to do with player
    /// damage stacking).
    ///
    /// Each new application of a debuff type adds an independent stack
    /// rather than overwriting/refreshing a single value. Stack damage/effect
    /// diminishes per the design: stack 1 = 100% of base, stack 2 = +50% of
    /// base, stack 3 = +25% of base, stack 4+ = +10% of base each. All
    /// stacks share ONE timer -- a new application refreshes that shared
    /// timer rather than each stack expiring independently.
    ///
    /// This struct only tracks COUNT and the shared TIMER -- it does NOT
    /// store each stack's individually-rolled damage value, since per the
    /// design, damage/effect strength is computed fresh from current
    /// StackCount via GetTotalMultiplier each time it's needed (tick time),
    /// not baked in at application time. This keeps the struct small and
    /// allocation-free, consistent with EnemyData's existing zero-allocation
    /// design.
    /// </summary>
    public struct DebuffStack
    {
        public int StackCount;
        public float SharedTimer;

        // The duration this timer was most recently set to -- needed so UI
        // can show an accurate radial countdown (SharedTimer / MaxDuration),
        // not just "still active or not." Updated every AddStack call, same
        // as SharedTimer itself (a fresh application resets both).
        public float MaxDuration;

        public bool IsActive => StackCount > 0 && SharedTimer > 0f;

        /// <summary>0 (just refreshed) to 1 (about to expire) -- inverse of a typical "fill up" cooldown, since this counts DOWN from full.</summary>
        public float RemainingFraction01 => MaxDuration > 0f ? Mathf.Clamp01(SharedTimer / MaxDuration) : 0f;

        /// <summary>Adds one stack and refreshes the shared timer to durationSeconds (not extended -- reset).</summary>
        public void AddStack(float durationSeconds)
        {
            StackCount++;
            SharedTimer = durationSeconds;
            MaxDuration = durationSeconds;
        }

        /// <summary>Call once per tick to decay the shared timer. Clears all stacks when it expires.</summary>
        public void Tick(float deltaTime)
        {
            if (SharedTimer <= 0f) return;

            SharedTimer -= deltaTime;
            if (SharedTimer <= 0f)
                StackCount = 0;
        }

        /// <summary>
        /// Sum of each stack's diminishing contribution, as a multiple of
        /// baseValue: stack 1 contributes 1.0x, stack 2 contributes +0.5x,
        /// stack 3 contributes +0.25x, every stack after that contributes
        /// +0.10x each. E.g. 4 stacks = 1.0 + 0.5 + 0.25 + 0.10 = 1.85x base.
        /// </summary>
        public float GetTotalMultiplier()
        {
            var total = 0f;
            for (var stackIndex = 1; stackIndex <= StackCount; stackIndex++)
                total += GetStackContribution(stackIndex);
            return total;
        }

        private static float GetStackContribution(int stackIndex)
        {
            switch (stackIndex)
            {
                case 1: return 1.0f;
                case 2: return 0.5f;
                case 3: return 0.25f;
                default: return 0.10f; // stack 4 onward
            }
        }
    }
}
