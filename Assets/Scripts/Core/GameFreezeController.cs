using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Single source of truth for Time.timeScale. Multiple systems can
    /// independently want the game frozen (level-up choice screen, pause
    /// menu, future cutscenes) — without this, the last system to write
    /// Time.timeScale = 1 would incorrectly unfreeze a different system's
    /// freeze that's still supposed to be active (e.g. unpausing while a
    /// level-up screen is open would resume gameplay underneath it).
    ///
    /// Each caller requests/releases a freeze under its own named reason;
    /// Time.timeScale is 0 whenever ANY reason is still active, 1 only when
    /// none are.
    ///
    /// Static, not a MonoBehaviour — there's exactly one Time.timeScale,
    /// so a singleton-via-static is the simplest correct shape here.
    /// </summary>
    public static class GameFreezeController
    {
        private static readonly HashSet<string> ActiveFreezeReasons = new();

        public static bool IsFrozen => ActiveFreezeReasons.Count > 0;

        public static void RequestFreeze(string reason)
        {
            ActiveFreezeReasons.Add(reason);
            Time.timeScale = 0f;
        }

        public static void ReleaseFreeze(string reason)
        {
            ActiveFreezeReasons.Remove(reason);
            if (ActiveFreezeReasons.Count == 0)
                Time.timeScale = 1f;
        }

        /// <summary>Call when the gameplay scene unloads, so a stale reason can't leak into the next session.</summary>
        public static void ClearAll()
        {
            ActiveFreezeReasons.Clear();
            Time.timeScale = 1f;
        }
    }
}
