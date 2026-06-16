using UnityEngine;

namespace Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothing = 8f;
        [SerializeField] private Vector3 offset = new(0f, 10f, 0f);

        private void LateUpdate()
        {
            if (!target) return;
            var desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing * Time.deltaTime);
        }
    }
}