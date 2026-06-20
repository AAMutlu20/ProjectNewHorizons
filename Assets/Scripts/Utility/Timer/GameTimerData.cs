using System;
using UnityEngine;

namespace Utility.Timer
{
    /// <summary>
    /// Pairs a GameTimer with the GameObject that currently owns it.
    /// Used by GameTimerControl to hand out reusable timers from a shared pool.
    /// </summary>
    [Serializable]
    public class GameTimerData
    {
        [SerializeField] private GameTimer timer;

        // Tracked as a GameObject reference (rather than an interface or id)
        // so a reservation automatically becomes invalid if the GameObject
        // is destroyed — Unity null-checks a destroyed reference as null.
        [SerializeField] private GameObject reservedBy;

        public GameTimer Timer => timer;

        public GameObject ReservedBy
        {
            get => reservedBy;
            set
            {
                reservedBy = value;
                ReleaseTimerState();
            }
        }

        public GameTimerData(float timerDuration)
        {
            timer = new GameTimer(timerDuration);
        }

        /// <summary>Clears listeners and resets the timer so the next owner starts from a clean state.</summary>
        private void ReleaseTimerState()
        {
            timer.ClearAllListeners();
            timer.Pause();
            timer.ResetElapsed();
            timer.Duration = 0f;
        }
    }
}
