using UnityEngine;

public class RotateVectorAroundPivot : MonoBehaviour
{
    [SerializeField] Vector3 _originaLocalPosition;
    [SerializeField] float _degreesToRotate;
    private void Start()
    {
        _originaLocalPosition = transform.localPosition;
    }

    public void RotateWithDegrees()
    {
        transform.localPosition = IrminStaticUtilities.Tools.EulerRotationUtility.RotateVector3AroundYWithDegrees(transform.localPosition, _degreesToRotate);
    }

    public void Reset()
    {
        transform.localPosition = _originaLocalPosition;
    }
}
