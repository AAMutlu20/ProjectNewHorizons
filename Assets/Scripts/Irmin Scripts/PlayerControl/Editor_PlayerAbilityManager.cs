using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerAbilityManager))]
public class Editor_PlayerAbilityManager : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PlayerAbilityManager playerAbilityManager = (PlayerAbilityManager)target;

        if(GUILayout.Button("Spawn Selected Ability"))
        {
            playerAbilityManager.SpawnSelectedAbility();
        }
    }
}
