#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IrminImageFlash))]
public class Editor_IrminImageFlash : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IrminImageFlash irminImageFlash = (IrminImageFlash)target;

        if(GUILayout.Button("FlashForSelectedSeconds"))
        {
            irminImageFlash.FlashImage();
        }
    }
}
#endif