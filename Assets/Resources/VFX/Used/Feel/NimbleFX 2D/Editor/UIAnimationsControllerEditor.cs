using UnityEditor;
namespace BhanuProductions.NimbleFX.Editor
{
    [CustomEditor(typeof(UIAnimations))]
    public class UIAnimationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            var animationTypeProp = serializedObject.FindProperty("animationType");
            AnimationType type = (AnimationType)animationTypeProp.enumValueIndex;
            EditorGUILayout.PropertyField(animationTypeProp);

            EditorGUILayout.Space(10);
            var targetSpriteProp = serializedObject.FindProperty("targettedSprite");
            EditorGUILayout.Space(10);
            var loopProp = serializedObject.FindProperty("autoStart");
            var loopDelayProp = serializedObject.FindProperty("loop");
            var autoStartProp = serializedObject.FindProperty("loopDelay");
            EditorGUILayout.PropertyField(targetSpriteProp);
            EditorGUILayout.PropertyField(loopProp);
            EditorGUILayout.PropertyField(loopDelayProp);
            EditorGUILayout.PropertyField(autoStartProp);

            var Action = serializedObject.FindProperty("onComplete");
            EditorGUILayout.PropertyField(Action);
            EditorGUILayout.Space(10);

            // Show variables conditionally
            switch (type)
            {
                case AnimationType.ShakeEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeIntensity"));
                    break;

                case AnimationType.BounceEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("bounceDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("bounceScale"));
                    break;

                case AnimationType.GlowEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("glowDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minAlpha"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAlpha"));
                    break;

                case AnimationType.RotateEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
                    break;
                case AnimationType.ScaleEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("targetScale"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleDuration"));

                    break;
                case AnimationType.SpriteSheetEffect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteFrames"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("frameRate"));
                    break;
                case AnimationType.ElasticResizing:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elasticTargetScale"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elasticDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elasticBounciness"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();


        }
    }
}