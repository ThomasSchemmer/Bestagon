using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(GameServiceWrapper))]
public class GameServiceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect Position, SerializedProperty ServiceProperty, GUIContent Label)
    {
        SerializedProperty EditorProperty = ServiceProperty.FindPropertyRelative("IsForEditor");
        SerializedProperty GameProperty = ServiceProperty.FindPropertyRelative("IsForGame");
        SerializedProperty ScriptProperty = ServiceProperty.FindPropertyRelative("TargetScript");

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(500));
        EditorGUILayout.PropertyField(ScriptProperty);
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(50));
        EditorGUILayout.LabelField("Editor", GUILayout.MaxWidth(40));
        EditorProperty.boolValue = EditorGUILayout.Toggle(EditorProperty.boolValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(50));
        EditorGUILayout.LabelField("Game", GUILayout.MaxWidth(40));
        GameProperty.boolValue = EditorGUILayout.Toggle(GameProperty.boolValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();
    }

}