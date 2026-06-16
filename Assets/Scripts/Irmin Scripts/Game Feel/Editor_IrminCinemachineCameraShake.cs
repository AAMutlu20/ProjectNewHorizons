#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IrminCinemachineCameraShake))]
public class Editor_IrminCinemachineCameraShake : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IrminCinemachineCameraShake irminCineMachineCameraShake = (IrminCinemachineCameraShake)target;

        if(GUILayout.Button("GenerateShakeWithSelectedVectorAtThisPosition"))
        {
            irminCineMachineCameraShake.DebugCameraShakeWithVelocityAtThisPosition();
        }

        if (GUILayout.Button("GenerateShakeWithSelectedVector"))
        {
            irminCineMachineCameraShake.DebugCameraShakeWithVelocity();
        }
    }
}
#endif