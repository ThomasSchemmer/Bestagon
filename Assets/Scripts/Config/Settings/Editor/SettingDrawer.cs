using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Setting))]
public class SettingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect Position, SerializedProperty SettingProperty, GUIContent Label)
    {
        var TypeProperty = SettingProperty.FindPropertyRelative("Type");
        var NameProperty = SettingProperty.FindPropertyRelative("Name");
        var ValueProperty = SettingProperty.FindPropertyRelative("Value");

        EditorGUILayout.BeginHorizontal();
        DrawValue(SettingProperty);
        EditorGUILayout.Space();
        GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25));
        EditorGUILayout.EndHorizontal();
    }

    protected virtual void DrawValue(SerializedProperty SettingProperty)
    {
        throw new NotImplementedException();
    }

    
}

[CustomPropertyDrawer(typeof(BooleanSetting))]
public class BooleanSettingDrawer : SettingDrawer
{
    protected override void DrawValue(SerializedProperty SettingProperty)
    {
        var NameProperty = SettingProperty.FindPropertyRelative("Name");
        var ValueProperty = SettingProperty.FindPropertyRelative("Value");

        string[] BoolValues = { "false", "true" };
        EditorGUILayout.PrefixLabel(NameProperty.name);
        ValueProperty.boolValue = EditorGUILayout.Popup(
            "Value",
            ValueProperty.boolValue ? 1 : 0,
            BoolValues
        ) > 0 ? true : false;
    }
}