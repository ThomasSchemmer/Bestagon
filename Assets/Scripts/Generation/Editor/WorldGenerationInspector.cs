using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGenerationInspector : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator Generator = (WorldGenerator)target;
        DrawDefaultInspector();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Land")))
        {
            Generator.NoiseLand(false, Generator.Seed);
        }

        if (GUILayout.Button(new GUIContent("Humidity")))
        {
            Generator.NoiseLand(true, Generator.Seed);
        }
        EditorGUILayout.EndHorizontal();

        if (Generator.InputRT != null)
        {
            GUILayout.Label(Generator.InputRT);
        }
        if (Generator.OutputRT != null)
        {
            GUILayout.Label(Generator.OutputRT);
        }
    }
}
