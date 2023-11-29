using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildingFactory))]
/*
 * Custom view for the BuildingFactory to enable reloading of scriptable building assets
 */ 
public class BuildingFactoryEditor : Editor
{
    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildingFactory Factory = (BuildingFactory)target;
        if (GUILayout.Button("Refresh"))
        {
            Factory.Refresh();
        }
    }
}
