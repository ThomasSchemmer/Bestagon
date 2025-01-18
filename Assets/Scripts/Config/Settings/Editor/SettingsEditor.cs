using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(Settings))]
public class SettingsEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var Settings = (Settings)target;
        
        string[] Types = Enum.GetNames(typeof(Setting.SettingType));
        int SelectedType = 0;

        EditorGUILayout.BeginHorizontal();
        SelectedType = EditorGUILayout.Popup(
            "Type:",
            SelectedType, 
            Types
        );
        bool bHasClicked = GUILayout.Button("Add");
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginVertical();
        var Prop = serializedObject.FindProperty("List");
        for (int i = 0; i < Prop.arraySize; i++) {
            var ElementProp = Prop.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(ElementProp);
        }
        EditorGUILayout.EndVertical();


        if (bHasClicked)
        {
            Settings.AddSetting((Setting.SettingType)SelectedType);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
