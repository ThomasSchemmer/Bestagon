using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

[CustomEditor(typeof(CloudRenderer))]

public class CloudRendererEditor : Editor
{
    override public void OnInspectorGUI()
    {
        CloudRenderer CloudRenderer = (CloudRenderer)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            CloudRenderer.CreateWhorleyNoise();
            EditorUtility.SetDirty(CloudRenderer.TargetTexture);
        }

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
