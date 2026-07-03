using Core;
using System;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Utility.Timer;


/// <summary>
/// Simple script to make the screen flash an image for a certain amount of time with transparency.
/// We are actually editing the transparency of the image with a timer.
/// </summary>
public class ImageFlasher : MonoBehaviour
{
    [SerializeField] private Image _imageToFlash;

    [SerializeField] private bool _startOnCooldown = true;
    [SerializeField] private GameTimer _cooldownTimer;
    [SerializeField] private bool _cooldownActive = false;
    [SerializeField] private GameTimer _imageTransparencyTimer = new();
    [SerializeField] private GameTimer _holdTimer = new();

    [SerializeField] private bool _hasToReverse = false;

    private float _fullTransparency = 0;
    [SerializeField] private float _targetTransparency = 1;

    private void Start()
    {
        _holdTimer.Completed += HoldTimerCompleted;
        _imageTransparencyTimer.Ticked += TransParencyTimerTick;
        _imageTransparencyTimer.Completed += TransparencyTimerCompleted;
        _cooldownTimer.Completed += CoolDownFinished;
        EventBus.Subscribe<PlayerHealthChangedEvent>(FlashOnHit);

        if(_startOnCooldown)
        {
            _cooldownTimer.Start();
        }
    }

    private void CoolDownFinished()
    {
        _cooldownActive = false;
    }

    public void StartCooldown()
    {
        _cooldownActive = true;
        _cooldownTimer.Start();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(FlashOnHit);
    }

    private void FlashOnHit(PlayerHealthChangedEvent @event)
    {
        Flash();
    }

    private void Update()
    {
        _holdTimer.UpdateTimer(Time.deltaTime);
        _imageTransparencyTimer.UpdateTimer(Time.deltaTime);
        _cooldownTimer.UpdateTimer(Time.deltaTime);
    }

    private void HoldTimerCompleted()
    {
        _imageTransparencyTimer.Start(true);
    }

    /// <summary>
    /// For our tick we want 1 to be fully visible and 0 to be completely transparent.
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="pPercentageUntillCompletion"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void TransParencyTimerTick(float arg1, float arg2, float pPercentageUntillCompletion)
    {
        Color newColor = _imageToFlash.color;
        newColor.a = (pPercentageUntillCompletion / 100) * _targetTransparency;
        _imageToFlash.color = newColor;
    }

    private void TransparencyTimerCompleted()
    {
        if(_hasToReverse)
        {
            _hasToReverse = false;
            _holdTimer.Start();
        }
    }

    public void Flash()
    {
        if (_cooldownActive)
        {
            return;
        }
        _hasToReverse = true;
        _imageTransparencyTimer.Start();
    }

}
