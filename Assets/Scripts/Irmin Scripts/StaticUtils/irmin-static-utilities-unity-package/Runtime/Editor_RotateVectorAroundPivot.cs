#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(RotateVectorAroundPivot))]
public class Editor_RotateVectorAroundPivot : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RotateVectorAroundPivot rotateVectorWithDegreesAroundPivotTest = (RotateVectorAroundPivot)target;

        if(GUILayout.Button("Rotate"))
        {
            rotateVectorWithDegreesAroundPivotTest.RotateWithDegrees();
        }

        if(GUILayout.Button("Reset"))
        {
            rotateVectorWithDegreesAroundPivotTest.Reset();
        }
    }
}
#endif