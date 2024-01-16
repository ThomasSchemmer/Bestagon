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
        if (GUILayout.Button(new GUIContent("Redo")))
        {
            Generator.NoiseLand();
        }
        if (Generator.EvenRT != null)
        {
            GUILayout.Label(Generator.EvenRT);
        }
    }
}
