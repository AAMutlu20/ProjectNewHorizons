using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillImageByIrminTimer : MonoBehaviour
{
    [SerializeField] private IrminTimer _irminTimerToFillImagesWith;
    [SerializeField] private List<Image> _imagesToFill = new List<Image>();
    [SerializeField] private bool _setFillImageGameObjectsByTimerActive = false;

    public IrminTimer IrminTimerToFillImagesWith { get { return _irminTimerToFillImagesWith; } set { BindIrminTimerEvents(false); _irminTimerToFillImagesWith = value; BindIrminTimerEvents(true); } }

    private void OnEnable()
    {
        BindIrminTimerEvents(true);
    }

    private void OnDisable()
    {
        BindIrminTimerEvents(false);
    }

    private void BindIrminTimerEvents(bool pBind)
    {
        if (_irminTimerToFillImagesWith == null) { return; }
        if(pBind)
        {
            _irminTimerToFillImagesWith.OnTimerTick += UpdateFillImagesByTimer;
            if (_setFillImageGameObjectsByTimerActive) { _irminTimerToFillImagesWith.OnTimerActiveChanged.AddListener(timerActiveSetFillImages); }
        }
        else
        {
            _irminTimerToFillImagesWith.OnTimerTick -= UpdateFillImagesByTimer;
            _irminTimerToFillImagesWith.OnTimerActiveChanged.AddListener(timerActiveSetFillImages);
        }
    }

    private void UpdateFillImagesByTimer(float pDeltaTime, float pCurrentTime, float pPercentage)
    {
        UpdateFillImages(pPercentage / 100);
    }

    private void UpdateFillImages(float pFillAmountValue)
    {
        for (int i = 0; i < _imagesToFill.Count; i++)
        {
            _imagesToFill[i].fillAmount = pFillAmountValue;
        }
    }

    /// <summary>
    /// Takes the values from the OnTimerActiveChangedEvent (old value and new value) and uses this the activate deactivate the fill images GameObjects.
    /// </summary>
    /// <param name="pOldValue"></param>
    /// <param name="pNewValue"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void timerActiveSetFillImages(bool pOldValue, bool pNewValue)
    {
        if (pNewValue != pOldValue) { SetAllFillImagesGameObjects(pNewValue); }
    }

    private void SetAllFillImagesGameObjects(bool pValue)
    {
        for (int i = 0; i < _imagesToFill.Count; i++)
        {
            _imagesToFill[i].gameObject.SetActive(pValue);
        }
    }
}
