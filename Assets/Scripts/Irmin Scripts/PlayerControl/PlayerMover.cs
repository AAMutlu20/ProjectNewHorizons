using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMover : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _playerNavMeshAgent;

    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _rayCastMaxDistance = 20;

    [SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _touchInputAction = null;
    [SerializeField] private string _touchInputActionName;

    [Header("Touching States")]
    [SerializeField] private bool _touching = false;

    private void Start()
    {
        GetInputActions();
        BindInputActions();
    }

    private void Update()
    {
        if(_touching)
        {
            UpdateMoveCharacter();
        }
    }

    private void GetInputActions()
    {
        _touchInputAction = _inputActionAsset.FindAction(_touchInputActionName);
        if (_touchInputAction == null)
        {
            Debug.LogError("PlayerMover.cs couldn't find input action for touch.");
        }
    }

    private void BindInputActions()
    {
        _touchInputAction.started += StartedTouch;
        _touchInputAction.canceled += EndedTouch;
    }

    private void EndedTouch(InputAction.CallbackContext context)
    {
        _touching = false;
        SetDestinationToPlayerPosition();
    }

    private void StartedTouch(InputAction.CallbackContext context)
    {
        _touching = true;
    }

    private void UpdateMoveCharacter()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Physics.Raycast(ray, out RaycastHit hit, _rayCastMaxDistance, _groundLayerMask);

        if(hit.collider != null)
        {
            _playerNavMeshAgent.SetDestination(hit.point);
        }
        else
        {
            SetDestinationToPlayerPosition();
        }
    }

    private void SetDestinationToPlayerPosition()
    {
        _playerNavMeshAgent.SetDestination(_playerNavMeshAgent.transform.position);
    }
}
