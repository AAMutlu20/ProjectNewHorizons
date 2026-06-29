using System;
using UnityEngine;
using Utility.Timer;

public class Pulser : MonoBehaviour
{
    [SerializeField] private GameTimer _pulseTimer;

    [SerializeField] Vector3 pulseScale;
    private Vector3 _defaultScale;

    private bool _reverse = false;

    private void Start()
    {
        _defaultScale = transform.localScale;
    }

    private void Update()
    {
        _pulseTimer.UpdateTimer(Time.deltaTime);
    }

    private void StartTimer()
    {
        _pulseTimer.Start();
    }

    private void SwitchReverse()
    {
        _reverse = !_reverse;
    }

    private void PauseTimer()
    {
        _pulseTimer.Pause();
    }

    private void UpdateScale(float arg1, float arg2, float pPercentageUntilCompletion)
    {
        if(!_reverse)
        {
            transform.localScale = Vector3.Lerp(_defaultScale, pulseScale, pPercentageUntilCompletion / 100);
        }
        else
        {
            transform.localScale = Vector3.Lerp(pulseScale, _defaultScale, pPercentageUntilCompletion / 100);
        }
    }

    private void OnEnable()
    {
        _pulseTimer.Ticked += UpdateScale;
        _pulseTimer.Completed += SwitchReverse;
        _pulseTimer.Completed += StartTimer;
        StartTimer();
    }

    private void OnDisable()
    {
        _pulseTimer.Ticked -= UpdateScale;
        _pulseTimer.Completed -= SwitchReverse;
        _pulseTimer.Completed -= StartTimer;
        PauseTimer();
    }
}
