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
        var NameProperty = SettingProperty.FindPropertyRelative("Name");
        var ValueProperty = SettingProperty.FindPropertyRelative("Value");

        DrawValue(SettingProperty);
    }

    protected virtual void DrawValue(SerializedProperty SettingProperty)
    {
        var TypeProperty = SettingProperty.FindPropertyRelative("_Type");
        switch (TypeProperty.intValue)
        {
            case 0: DrawBooleanValue(SettingProperty); break;
            case 1: DrawIntValue(SettingProperty); break;
        }
    }


    protected void DrawBooleanValue(SerializedProperty SettingProperty)
    {
        var ValueProperty = SettingProperty.FindPropertyRelative("Value");

        string[] BoolValues = { "false", "true" };
        ValueProperty.intValue = EditorGUILayout.Popup(
            ValueProperty.intValue,
            BoolValues
        );
    }

    protected void DrawIntValue(SerializedProperty SettingProperty)
    {
        var ValueProperty = SettingProperty.FindPropertyRelative("Value");
        var MinValueProperty = SettingProperty.FindPropertyRelative("MinValue");
        var MaxValueProperty = SettingProperty.FindPropertyRelative("MaxValue");

        EditorGUILayout.BeginVertical();
        ValueProperty.intValue = EditorGUILayout.DelayedIntField(
            ValueProperty.intValue
        );
        EditorGUILayout.BeginHorizontal();
        float Old = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 35;
        EditorGUILayout.PrefixLabel("Min:");
        MinValueProperty.intValue = EditorGUILayout.DelayedIntField(
            MinValueProperty.intValue
        );
        EditorGUILayout.PrefixLabel("Max:");
        MaxValueProperty.intValue = EditorGUILayout.DelayedIntField(
            MaxValueProperty.intValue
        );
        ValueProperty.intValue = Mathf.Clamp(ValueProperty.intValue, MinValueProperty.intValue, MaxValueProperty.intValue);
        EditorGUIUtility.labelWidth = Old;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
}