using System;
using UnityEngine;
using Utility.Timer;

// Has methods to scale UI to a pre defined scale or a custom scale.
// Uses the average distance between two scales as duration input for the animation timer.

public class UIScaler : MonoBehaviour
{
    Vector3 _defaultScale;
    [SerializeField] Vector3 _scaledScale;

    [SerializeField] private GameTimer _scaleTimer;
    [SerializeField] private Vector3 _oldScale;
    [SerializeField] private Vector3 _newScale;

    public bool IsAnimating { get { return _scaleTimer.IsRunning; } }

    private void Start()
    {
        _scaleTimer.Ticked += UpdateScaleWithGameTimer;
        _defaultScale = transform.localScale;
    }

    //Args: deltaTime, elapsedTime, percentageComplete.
    private void UpdateScaleWithGameTimer(float pDeltaTime, float pElapsedTime, float pPercentageComplete)
    {
        Vector3 newScale = new();
        newScale.x = Mathf.Lerp(_oldScale.x, _newScale.x, (pPercentageComplete / 100));
        newScale.y = Mathf.Lerp(_oldScale.y, _newScale.y, (pPercentageComplete / 100));
        newScale.z = Mathf.Lerp(_oldScale.z, _newScale.z, (pPercentageComplete / 100));
        transform.localScale = newScale;
    }

    private void Update()
    {
        _scaleTimer.UpdateTimer(Time.deltaTime);
    }

    private void ScaleWithDifference(Vector3 pOldScale, Vector3 pNewScale)
    {
        _oldScale = pOldScale;
        _newScale = pNewScale;
        float averageDifference = Mathf.Abs(((_oldScale.x + _oldScale.y + _oldScale.z) / 3) - ((_newScale.x + _newScale.y + _newScale.z) / 3));
        _scaleTimer.Duration = averageDifference;
        _scaleTimer.Start();
    }

    public void ScaleToDefaultScale()
    {
        ScaleWithDifference(transform.localScale, _defaultScale);
    }

    public void ScaleToHoveredScale()
    {
        ScaleWithDifference(transform.localScale, _scaledScale);
    }

    public void ScaleToScale(Vector3 pNewScale)
    {
        ScaleWithDifference(transform.localScale, pNewScale);
    }

}
