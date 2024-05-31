using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CloudRenderer))]

public class CloudRendererEditor : Editor
{
    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CloudRenderer CloudRenderer = (CloudRenderer)target;
        if (GUILayout.Button("Refresh"))
        {
            CloudRenderer.CreateWhorleyNoise();
        }
        if (GUILayout.Button("Tile"))
        {
            CloudRenderer.Tile();
        }
        if (GUILayout.Button("Debug"))
        {
            CloudRenderer.Debug();
        }
        if (GUILayout.Button("Clear"))
        {
            CloudRenderer.Clear();
        }
    }
}
