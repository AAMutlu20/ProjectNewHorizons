using System;
using UnityEngine;

namespace IrminTimerPackage.Tools
{
    [Serializable]
    public class IrminTimerData
    {
        [SerializeField] private IrminTimer _irminTimer;
        // I made this a GameObject for debug purposes, It will bind the reservation of this timer to a GameObject and as added benefit will autimatically stop the reservation if the GameObject is destroyed because it would return null. If anything strange would be going on with the reservations, I can see which reservation are bound to inactive or strange objects and find a solution that way.
        [SerializeField] private GameObject _reservedBy;

        public IrminTimer IrminTimer { get { return _irminTimer; } set { _irminTimer = value; } }
        /// <summary>
        /// Sets the gameobject this IrminTimer is reserver for. Be aware that this clears all timer events and resets the timer.
        /// </summary>
        public GameObject ReservedBy { get { return _reservedBy; } set { _reservedBy = value; _irminTimer.ClearAllEvents(); _irminTimer.PauseTimer(); _irminTimer.ResetCurrentTime(); _irminTimer.Time = 0; } }

        public IrminTimerData(float pTimerTime) { _irminTimer = new(); }
         
    }
}