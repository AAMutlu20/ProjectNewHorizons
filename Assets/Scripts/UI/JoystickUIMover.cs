using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class JoystickUIMover : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActionAsset;
    [SerializeField] private string _touchInputActionName;
    [SerializeField] private InputAction _touchInputAction;

    [SerializeField] private RectTransform _uiToMove;

    [SerializeField] private List<GameObject> _images = new();

    Coroutine _currentShowUICoroutine;

    private void Start()
    {
        _touchInputAction = _inputActionAsset.FindAction(_touchInputActionName);
        _touchInputAction.started += CheckToMoveUIElement;
        //_touchInputAction.started += ShowUIToMove;
        //_touchInputAction.canceled += HideUIToMove;
    }

    private IEnumerator ShowUIAfterAFrame()
    {
        yield return null;
        SetImages(true);
        _currentShowUICoroutine = null;
        Debug.Log("Showing Stick");
    }

    private void SetImages(bool pValue)
    {
        for (int i = 0; i < _images.Count; i++)
        {
            _images[i].SetActive(pValue);
        }
    }

    private void CheckToMoveUIElement(InputAction.CallbackContext context)
    {
        // Check if we are already hovering the UI
        //if (EventSystem.current.IsPointerOverGameObject())
        //{
        //    return;
        //}
        // If not we move the movement UI element
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Input.touches.Length <= 0 )
        {
            return;
        }
        Touch lastTouch = Input.GetTouch(Input.touchCount - 1);
        // Access the screen position of the last touch
        Vector2 lastTouchScreenPosition = lastTouch.position;


        _uiToMove.position = lastTouchScreenPosition;

        Debug.Log("Moving Joystick");
    }
}