using Unity.Mathematics;
using UnityEngine;

public class MyCuteVisionConeVisualizer : MonoBehaviour
{
    [SerializeField] MyCuteVectorTest _myCuteVectorTest;

    [SerializeField] private float _viewDistance = 10.0f;
    // UNUSED[SerializeField] private float _fov = 90.0f;
    //[SerializeField] private Color _gizmoColor = Color.white;

    private void OnDrawGizmosSelected()
    {
        // Draw a forward line
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * _viewDistance);

        // Left boundary
        // Explanation: When you multiply a quaternion by a vector, you are: Rotating that vector by that rotation.
        Vector3 leftDirection = Quaternion.Euler(0, -_myCuteVectorTest.HalfFOV, 0) * (transform.TransformPoint(Vector3.forward) - transform.position);
        Gizmos.DrawLine(transform.position, transform.position + leftDirection * _viewDistance);
        // Right boundary
        Vector3 rightDirection = Quaternion.Euler(0, _myCuteVectorTest.HalfFOV, 0) * (transform.TransformPoint(Vector3.forward) - transform.position);
        Gizmos.DrawLine(transform.position, transform.position + rightDirection * _viewDistance);
    }
}
