using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MatrixTest : MonoBehaviour
{

    [SerializeField] private Vector3 _positionForMatrix = Vector3.zero;
    [SerializeField] private Vector3 _rotationForMatrixAsEuler;
    [SerializeField] private Vector3 _scaleForMatrix = Vector3.one;

    Matrix4x4 _translationMatrixToTest;

    [SerializeField] private List<Vector3> _points = new();
    [SerializeField] private GameObject _pointPrefab;
    [SerializeField] private List<GameObject> _spawnedPointGameObjects = new();

    [SerializeField] private bool _overrideMatrixPositionToObjectPositionAtStart = false;

    // Translation is done with shearing https://www.youtube.com/watch?v=Do_vEjd6gF0

    private void Start()
    {
        if (_overrideMatrixPositionToObjectPositionAtStart) { _positionForMatrix = transform.position; }
        for (int i = 0; i < _points.Count; i++)
        {
            _spawnedPointGameObjects.Add(Instantiate(_pointPrefab));
        } 
    }

    private void Update()
    {
        _translationMatrixToTest = Matrix4x4.TRS(_positionForMatrix, Quaternion.Euler(_rotationForMatrixAsEuler), _scaleForMatrix);
        for (int i = 0; i < _spawnedPointGameObjects.Count; i++)
        {
            _spawnedPointGameObjects[i].transform.position = _translationMatrixToTest.MultiplyPoint3x4(_points[i]);
        }
    }


    // MultiplyPoint3x4 for standard translation and MultiplyPoint for translation with perspective (camera)
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            Gizmos.DrawSphere(_translationMatrixToTest.MultiplyPoint3x4(_points[i]) , 0.5f);
        }
    }
}
