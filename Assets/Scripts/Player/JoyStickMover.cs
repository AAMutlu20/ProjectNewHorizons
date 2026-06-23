using Player;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class JoyStickMover : MonoBehaviour
{
    [SerializeField] private VirtualJoystick _virtualJoyStick;
    [SerializeField] private InputActionAsset _inputActionAsset;
    [SerializeField] private string _moveInputActionName;
    [SerializeField] private InputAction _moveInputAction;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    // Replaced by:
    [SerializeField] PlayerController _playerController;

    [SerializeField] private float _speed;

    bool _moving = false;

    private void Start()
    {
        _moveInputAction = _inputActionAsset.FindAction(_moveInputActionName);
        _moveInputAction.started += Started;
        _moveInputAction.canceled += Canceled;
    }

    private void Canceled(InputAction.CallbackContext context)
    {
        Debug.Log("Move cancelled");
        _moving = false;
    }

    private void Started(InputAction.CallbackContext context)
    {
        Debug.Log("Move started");
        _moving = true;
    }

    private void Update()
    {
        if (_moving)
        {
            Vector2 input = _moveInputAction.ReadValue<Vector2>();
            //Debug.Log(input);
            //Vector3 newMoveVector = new Vector3(input.x, 0, input.y);
            //newMoveVector.Normalize();
            //newMoveVector *= _speed;
            //_navMeshAgent.transform.rotation = Quaternion.LookRotation(newMoveVector);
            //_navMeshAgent.Move(newMoveVector);
            Debug.Log($"Movement Input Update {input}");
        }
    }
}
