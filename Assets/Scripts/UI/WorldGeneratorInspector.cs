using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorInspector : Editor
{
    int Amount = 2;
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        WorldGenerator Script = (WorldGenerator)target;

        string AmountString = GUILayout.TextField(""+ Amount);
        Amount = int.Parse(AmountString);
        if (GUILayout.Button("Execute")) {
            Script.Execute();
        }
        if (GUILayout.Button("Move")) {
            if (Amount >= 0) {
                Script.Move(Amount);
            } else {
                Script.Move();
            }
        }

        GUILayout.Label(Script.EvenRT);
        GUILayout.Label(Script.OddRT);
    }
}
