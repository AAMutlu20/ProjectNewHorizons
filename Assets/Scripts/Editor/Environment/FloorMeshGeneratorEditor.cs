using Environment;
using UnityEditor;
using UnityEngine;

namespace EditorTools.Environment
{
    /// <summary>
    /// Custom Inspector for FloorMeshGenerator -- adds a visible "Generate
    /// Floor Mesh" button below the normal fields, calling the same
    /// Generate() the [ContextMenu] attribute already exposes, just without
    /// needing to hunt through the right-click/three-dot menu.
    ///
    /// Marks the generated mesh/scene dirty so the bake is actually saved
    /// with the scene -- without this, a mesh assigned only at runtime
    /// (or via a context menu call that doesn't dirty anything) can silently
    /// vanish on the next scene reload since Unity has no reason to think
    /// anything changed.
    ///
    /// Lives in an "Editor" folder (required by Unity -- anything under a
    /// folder literally named Editor is excluded from player builds).
    /// </summary>
    [CustomEditor(typeof(FloorMeshGenerator))]
    public class FloorMeshGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Floor Mesh", GUILayout.Height(30)))
                GenerateAndDirty();
        }

        private void GenerateAndDirty()
        {
            var generator = (FloorMeshGenerator)target;

            Undo.RecordObject(generator, "Generate Floor Mesh");
            generator.Generate();

            // Without this, the newly-baked mesh/collider only exist in memory
            // for this Editor session -- closing or reloading the scene
            // without an explicit save would silently lose the bake.
            EditorUtility.SetDirty(generator);
            if (!Application.isPlaying)
                MarkActiveSceneDirty();
        }

        private static void MarkActiveSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
