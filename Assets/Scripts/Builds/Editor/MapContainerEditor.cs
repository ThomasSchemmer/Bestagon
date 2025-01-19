using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapContainer))]
public class MapContainerEditor : Editor
{

    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!GUILayout.Button("Load"))
            return;

        MapContainer Container = (MapContainer)target;
        string Path = EditorUtility.OpenFilePanel("Select map file", SaveGameManager.GetSavegamePath(), "");
        Container.MapData = File.ReadAllBytes(Path);
        EditorUtility.SetDirty(Container);
    }
}
