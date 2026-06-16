using Unity.Burst.Intrinsics;
using UnityEngine;

public class MyCuteVectorTest : MonoBehaviour
{
    /*
     * Pythagoras formula:
     * Get length of vector
     * Square root of (x squared + y squared)
     * 
     * Dot product:
     * Maaagic
     * Input is two directions. The dot product tells you how aligned those two directions are
     * 
     * Sin, CoSin
     * if you hava a radian angle, an angle of zero point to the right
     * sin gives y, cosin gives x
     * 
     * 
     * Unit vector
     * Divide the vector by its length
     * x / length
     * y / length
     * z / length
     * 
     * How to calculate the angle of a vector?
     * float angleInRadians = Mathf.Atan2(Vector.y, Vector.x);
     * 
    */


    [SerializeField] Transform _dotProductTransform1;
    [SerializeField] float _fov;
    [SerializeField] float _halfFOV;
    [SerializeField] float _dotThreshold;
    [SerializeField] float _distanceRange = 30.0f;
    [SerializeField] float _distanceToTarget = -1;
    [SerializeField] Vector3 _directionToTargetNormalized;
    [SerializeField] Transform _dotProductTransform2;


    [SerializeField] private bool _dotProductTransform2detected = false; 

    // Light is calculated by using the dot product on the light direction and normals
    // You should explain the dot product in 3D
    // Dot product can be written as v.w = vx * wx + vy * wy + vz * wz
    // You multiply the on vector with the other and the result is a scalar
    // For example:
    // V1 = (1, 0, 0)
    // V2 = (-1, 0, 0)
    // Result = 1 * -1 = -1, 0 * 0 = 0, 0 * 0 = 0
    // -1 + 0 + 0 = -1
    // -1
    //
    // Another calculation?:
    // v.w = length of v * lenght of w * cos angle
    public float HalfFOV { get { return _halfFOV; } }
    public float DistanceToTarget { get { return _distanceToTarget; } }
    public Vector3 DirectionToTargetNormalized { get { return _directionToTargetNormalized; } }
    public bool DotProductTransform2detected { get { return _dotProductTransform2detected; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float magnitude = new Vector3(-3, -4, 0).magnitude;
        //Debug.Log(magnitude);
        //Debug.Log($"{-3 / magnitude}, {-4 / magnitude}, {0 / magnitude}");
        //Debug.Log(new Vector3(-3, -4, 0).normalized);

        float angleInRadians = Mathf.Atan2(-4, -3);
        //Debug.Log(angleInRadians);
        //Debug.Log(angleInRadians * 360 / (2f * Mathf.PI));
        //Debug.Log(angleInRadians * Mathf.Rad2Deg);

        float secondAngleInRadians = Mathf.Atan2(0, -1);
        float secondAngleInDegrees = secondAngleInRadians * Mathf.Rad2Deg;
        //Debug.Log(secondAngleInRadians);
        //Debug.Log(secondAngleInDegrees);


        // Excersize:
        // 1
        // V1 = 3,4
        // V2 = -5,4
        // 3 * - 5 = -15
        // 4 * 4 = 16
        // result = -15 + 16 = 1
        //
        // 2
        // V1 = 2,3
        // V2 = 4,-3
        // 2 * 4 = 8
        // 3 * -3 = -9
        // result = -1
        //
        // 3
        // V1 = -1,2 (this is the angle of the players view)
        // V2 = -3,-1 (this is the angle of the player compared to the enemy so player pos - enemy pos)
        // -1 * -3 = 3
        // 2 * -1 = -2
        // result = 3 + -2 = 1
        //float dot = Vector3.Dot(new Vector3(-1, 2), new Vector3(-3, -1));
        //Debug.Log(dot);
        //
        // 4
        // 

        
    }

    // Update is called once per frame
    void Update()
    {
        CalculateDotToCheckIfBehindOrInFront();
    }

    public void CalculateAndSetDotThreshold()
    {
        _halfFOV = _fov * 0.5f;
        // This works because: dot = cos(angle between vectors)
        _dotThreshold = Mathf.Cos(_halfFOV * Mathf.Deg2Rad);
    }

    public void CalculateDotToCheckIfBehindOrInFront()
    {
        CalculateAndSetDotThreshold();
        _directionToTargetNormalized = (_dotProductTransform2.position -_dotProductTransform1.position).normalized;
        // return a number lower than 1 if the second transform is behind the first transform or a number higher than 1 if the second transform is in front of the first transform
        Debug.Log($"Dot product of vec1{_dotProductTransform1.forward} and vec2{_dotProductTransform2.position - _dotProductTransform1.position} = {Vector3.Dot(_dotProductTransform1.forward, _directionToTargetNormalized)}");
        Debug.Log($"With the vision angles taken into account the result will be {Vector3.Dot(_dotProductTransform1.forward, _directionToTargetNormalized)} is > than dot threshold {_dotThreshold} {Vector3.Dot(_dotProductTransform1.forward, _directionToTargetNormalized) > _dotThreshold}");
        _distanceToTarget = Vector3.Distance(_dotProductTransform1.position, _dotProductTransform2.position);
        _dotProductTransform2detected = Vector3.Dot(_dotProductTransform1.forward, _directionToTargetNormalized) > _dotThreshold && _distanceToTarget <= _distanceRange;

        //Debug.Log($"Dot product of vec1{_dotProductVector1} and vec2{_dotProductVector2} = {Vector3.Dot(_dotProductVector1.normalized, _dotProductVector2.normalized)}");
    }
}
