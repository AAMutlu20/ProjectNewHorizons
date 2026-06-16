using Unity.Cinemachine;
using UnityEngine;

public class IrminCinemachineCameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource _cineMachineImpulsSourceToPlay;

    [SerializeField] private Vector3 _debugCameraShakeVelocity = Vector3.up;
    


    public void CameraShakeWithVelocityAtThisPosition(Vector3 pVelocity)
    {
        _cineMachineImpulsSourceToPlay.GenerateImpulseAtPositionWithVelocity(transform.position, pVelocity);
    }

    public void CameraShakeWithVelocity(Vector3 pVelocity)
    {
        _cineMachineImpulsSourceToPlay.GenerateImpulseWithVelocity(pVelocity);
    }

    public void DebugCameraShakeWithVelocityAtThisPosition()
    {
        CameraShakeWithVelocityAtThisPosition(_debugCameraShakeVelocity);
    }

    public void DebugCameraShakeWithVelocity()
    {
        CameraShakeWithVelocity(_debugCameraShakeVelocity);
    }

}
