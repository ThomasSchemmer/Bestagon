using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(Settings))]
public class SettingsEditor : Editor
{
    int SelectedType = 0;

    public override void OnInspectorGUI()
    {
        string[] Types = Enum.GetNames(typeof(Setting.Type));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type: ", GUILayout.Width(50));
        SelectedType = EditorGUILayout.Popup(
            SelectedType,
            Types,
            GUILayout.Width(150)
        );
        EditorGUILayout.Space();
        bool bHasClicked = GUILayout.Button("Add");
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        var Entries = serializedObject.FindProperty("Entries");
        var Tuples = Entries.FindPropertyRelative("Tuples");
        
        for (int i = 0; i < Tuples.arraySize; i++)
        {
            SerializedProperty ElementProp = Tuples.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            var KeyProp = ElementProp.FindPropertyRelative("Key");
            KeyProp.intValue = EditorGUILayout.Popup(
                KeyProp.intValue,
                KeyProp.enumDisplayNames,
                GUILayout.Width(125)
            );

            var ValueProp = ElementProp.FindPropertyRelative("Value");
            EditorGUILayout.PropertyField(ValueProp, GUILayout.Width(0));
            if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
            {
                Tuples.DeleteArrayElementAtIndex(i);
                Tuples.serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndVertical();

        if (bHasClicked)
        {
            Settings.Get().Add(SettingName.DEFAULT, (Setting.Type)SelectedType);
        }
        serializedObject.ApplyModifiedProperties();
    }
}