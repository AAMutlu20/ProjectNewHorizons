using System.Collections.Generic;
using UnityEngine;

namespace Utility.Timer
{
    /// <summary>
    /// Pools reusable GameTimers so gameplay systems can request a timer
    /// without instantiating one each time. A reserved timer is exclusive
    /// to its owner until EndReservation is called.
    ///
    /// Attach to: a single GameObject in [Systems]. Mark isSingleton if
    /// only one GameTimerControl should exist in the scene.
    /// </summary>
    public class GameTimerControl : MonoBehaviour
    {
        private static GameTimerControl Singleton { get; set; }

        [SerializeField] private bool isSingleton;
        [SerializeField] private List<GameTimerData> timerPool = new();

        private void Awake()
        {
            TryRegisterSingleton();
        }

        private void Update()
        {
            foreach (var t in timerPool)
                t.Timer.UpdateTimer(Time.deltaTime);
        }

        private void TryRegisterSingleton()
        {
            if (!isSingleton) return;

            if (Singleton && Singleton != this)
            {
                Debug.LogWarning(
                    $"GameTimerControl: cannot register '{name}' as Singleton — " +
                    $"'{Singleton.name}' already holds that role.", this);
                isSingleton = false;
                return;
            }

            Singleton = this;
        }

        /// <summary>
        /// Reserves a timer for the given owner, reusing an idle pooled timer
        /// if one is available or creating a new one otherwise. The timer is
        /// exclusive to owner until EndReservation is called with the
        /// returned key.
        /// </summary>
        public bool TryReserveTimer(GameObject owner, float duration, out int timerKey, out GameTimer timer)
        {
            if (owner == null)
            {
                Debug.LogWarning("GameTimerControl: cannot reserve a timer for a null owner.", this);
                timerKey = -1;
                timer = null;
                return false;
            }

            var pooledIndex = FindIdleTimerIndex();
            if (pooledIndex >= 0)
            {
                timerKey = pooledIndex;
                timer = StartReservedTimer(timerPool[pooledIndex], owner, duration);
                return true;
            }

            var newTimerData = new GameTimerData(duration);
            timerPool.Add(newTimerData);
            timerKey = timerPool.Count - 1;
            timer = StartReservedTimer(newTimerData, owner, duration);
            return true;
        }

        public void EndReservation(int timerKey)
        {
            if (timerKey < 0 || timerKey >= timerPool.Count)
            {
                Debug.LogWarning($"GameTimerControl: timer key {timerKey} is out of range.", this);
                return;
            }

            timerPool[timerKey].ReservedBy = null;
        }

        private int FindIdleTimerIndex()
        {
            for (var i = 0; i < timerPool.Count; i++)
            {
                if (!timerPool[i].Timer.IsRunning && !timerPool[i].ReservedBy)
                    return i;
            }
            return -1;
        }

        private static GameTimer StartReservedTimer(GameTimerData timerData, GameObject owner, float duration)
        {
            timerData.ReservedBy = owner;
            timerData.Timer.Duration = duration;
            timerData.Timer.Start();
            return timerData.Timer;
        }
    }
}
