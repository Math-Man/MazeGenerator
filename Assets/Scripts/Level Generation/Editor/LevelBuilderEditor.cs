#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Level_Generation.Editor
{
    [CustomEditor(typeof(LevelBuilder))]
    public class LevelBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelBuilder levelBuilder = (LevelBuilder) target;
            if (GUILayout.Button("Generate"))
            {
                levelBuilder.BuildLevel();
            }
            
            if (GUILayout.Button("Clear"))
            {
                levelBuilder.ClearLevel();
            }
        }
    }
}
#endif