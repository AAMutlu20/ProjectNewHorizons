using IrminTimerPackage.Tools;
using System;
using UnityEngine;
using UnityEngine.UI;

public class IrminImageFlash : MonoBehaviour
{
    [SerializeField] private Image _imageToFlash;
    [SerializeField] IrminTimer _flashTimer = new();
    [SerializeField] float _maxAlpha;
    [SerializeField] Color _originalColor;

    private void Start()
    {
        _flashTimer.OnTimeElapsed += RevertTimer;
        _flashTimer.OnTimerTick += UpdateFlashAnimation;
        _flashTimer.OnTimeElapsedReverse += ResetImageColor;
        _originalColor = _imageToFlash.color;
    }

    private void ResetImageColor()
    {
        _imageToFlash.color = _originalColor;
    }

    private void Update()
    {
        _flashTimer.UpdateTimer(Time.deltaTime);
    }

    private void UpdateFlashAnimation(float pDeltaTime, float pCurrentTime, float pPercentage)
    {
        Color foundColorToEdit = _imageToFlash.color;
        foundColorToEdit.a = (_maxAlpha / 100) * pPercentage;
        Debug.Log($"Setting image colro alpha to {(_maxAlpha / 100) * pPercentage}");
        _imageToFlash.color = foundColorToEdit;
    }

    private void RevertTimer()
    {
        _flashTimer.StartTimer(true);
    }

    public void FlashImage()
    {
        _flashTimer.StartTimer();
    }
}
