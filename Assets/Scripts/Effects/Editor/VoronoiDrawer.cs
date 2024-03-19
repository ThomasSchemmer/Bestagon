using System;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

[CustomEditor(typeof(VoronoiTest))]
public class VoronoiDrawer : Editor
{

    public override void OnInspectorGUI()
    {
        VoronoiTest Voronoi = (VoronoiTest)target;
        DrawDefaultInspector();
        EditorGUILayout.BeginHorizontal();

        Texture2D myTexture = AssetPreview.GetAssetPreview(Voronoi.TargetTexture);
        GUILayout.Label(myTexture);

        EditorGUILayout.EndHorizontal();



    }
}