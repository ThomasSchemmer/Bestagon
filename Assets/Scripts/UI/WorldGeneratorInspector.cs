using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorInspector : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        WorldGenerator Script = (WorldGenerator)target;

        string AmountString = GUILayout.TextField("1");
        int Amount = int.Parse(AmountString);
        if (GUILayout.Button("Execute")) {
            Script.Execute();
        }
        if (GUILayout.Button("Move")) {
            Script.Move(Amount);
        }

        GUILayout.Label(Script.EvenRT);
        GUILayout.Label(Script.OddRT);
    }
}
