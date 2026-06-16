using IrminTimerPackage.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace IrminTimerPackage.Tools
{
    public class IrminTimerControl : MonoBehaviour
    {
        public static IrminTimerControl Singleton;

        [SerializeField] private bool _isSingleton = false;
        [SerializeField] List<IrminTimerData> _irminTimersData = new();

        private void Awake()
        {
            SetSingletonIfAssigned();
        }

        private void Update()
        {
            for (int i = 0; i < _irminTimersData.Count; i++)
            {
                _irminTimersData[i].IrminTimer.UpdateTimer(Time.deltaTime);
            }
        }

        private void SetSingletonIfAssigned()
        {
            if (!_isSingleton) return;
            if (Singleton != null && Singleton != this) { Debug.LogWarning($"Couldn't set {name} to be IrminTimerControl Singleton because a singleton already exists called {Singleton.name}"); _isSingleton = false; return; }
            Singleton = this;
        }

        /// <summary>
        /// Gets or creates and sets a timer. Please be aware that the timer is "reserved" meaning no other object can use until the EndTimerReservation method for it is called.
        /// </summary>
        /// <param name="pReservingGameObject">The GameObject this timer has been reserved for.</param>
        /// <param name="pTime">The time until the timer elapses.</param>
        /// <param name="pTimerKey"> The key index of the timer in the IrminTimerControl.</param>
        /// <param name="pIrminTimer">A reference to the IrminTimer. You could use this to subscribe to its events like OnTimerTick and OnTimeElapsed.</param>
        public void ReserveTimer(GameObject pReservingGameObject, float pTime, out int pTimerKey, out IrminTimer pIrminTimer)
        {
            if (pReservingGameObject == null) { Debug.LogWarning("IrminTimerControl WARNING: Reserving GameObject cannot be null, cancelling."); pTimerKey = -1; pIrminTimer = null; return; }
            // Search for an available timer and use that timer.
            for (int i = 0; i < _irminTimersData.Count; i++)
            {
                if (!_irminTimersData[i].IrminTimer.TimerActive && _irminTimersData[i].ReservedBy == null)
                {
                    _irminTimersData[i].ReservedBy = pReservingGameObject;
                    // If this timer is unreserved, all events should be cleared. You should reserve a timer to not have to reassign events.

                    pTimerKey = i;
                    pIrminTimer = _irminTimersData[i].IrminTimer;
                    _irminTimersData[i].IrminTimer.Time = pTime;
                    _irminTimersData[i].IrminTimer.StartTimer();

                    
                    return;
                }
            }
            // No available timer was found, create a new one to use
            IrminTimerData newTimerData = new(pTime);
            _irminTimersData.Add(newTimerData);
            pTimerKey = _irminTimersData.Count - 1;
            pIrminTimer = newTimerData.IrminTimer;
            //newTimerData.IrminTimer.Time = pTime;
            newTimerData.IrminTimer.StartTimer();

            newTimerData.ReservedBy = pReservingGameObject;
        }

        public void EndTimerReservation(int pTimerKey)
        {
            _irminTimersData[pTimerKey].ReservedBy = null;
        }
    }
}

