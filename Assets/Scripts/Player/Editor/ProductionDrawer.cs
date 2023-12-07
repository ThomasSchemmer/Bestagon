using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static Codice.CM.Common.CmCallContext;

[CustomPropertyDrawer(typeof(Production))]
public class ProductionDrawer : PropertyDrawer
{
    List<SerializedProperty> Tuples;

    public override void OnGUI(Rect Position, SerializedProperty ProductionProperty, GUIContent Label)
    {
        SerializedProperty _ProductionProperty = ProductionProperty.FindPropertyRelative("_Production");
        SerializedProperty TuplesProperty = _ProductionProperty.FindPropertyRelative("Tuples");

        Tuples = new List<SerializedProperty>();
        for (int i = 0; i < TuplesProperty.arraySize; i++)
        {
            Tuples.Add(TuplesProperty.GetArrayElementAtIndex(i));
        }

        EditorGUILayout.PrefixLabel(ProductionProperty.displayName);
        EditorGUI.indentLevel++;

        DrawElements();

        EditorGUI.indentLevel--;
    }

    private void DrawElements()
    {
        float Width = EditorGUIUtility.currentViewWidth - 50;

        string[] Resources = Enum.GetNames(typeof(Production.Type));
        EditorGUILayout.BeginHorizontal();
        foreach (SerializedProperty TupleProperty in Tuples)
        {
            SerializedProperty KeyProperty = TupleProperty.FindPropertyRelative("Key");
            SerializedProperty ValueProperty = TupleProperty.FindPropertyRelative("Value");

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(
                Resources[KeyProperty.intValue],
                GUILayout.MaxWidth(Width / Tuples.Count)
            );
            ValueProperty.intValue = EditorGUILayout.IntField(
                ValueProperty.intValue,
                GUILayout.MaxWidth(Width / Tuples.Count)
            );
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    public static void PrintProp(SerializedProperty ParentProp)
    {
        var Iterator = ParentProp.GetEnumerator();
        while (Iterator.MoveNext())
        {
            SerializedProperty Current = (SerializedProperty)Iterator.Current;
            Debug.Log(Current.name + " " + Current.propertyPath + " " + Current.depth);
        }
    }
}