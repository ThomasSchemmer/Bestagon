using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(ProductionProperty.displayName);
        if (GUILayout.Button(new GUIContent("+"), GUILayout.MaxWidth(25)))
        {
            int MaxValue = Enum.GetNames(typeof(Production.Type)).Length;
            TuplesProperty.InsertArrayElementAtIndex(TuplesProperty.arraySize);

            var NewProp = TuplesProperty.GetArrayElementAtIndex(TuplesProperty.arraySize - 1).FindPropertyRelative("Key");
            if (TuplesProperty.arraySize >= 2)
            {
                var PrevProp = TuplesProperty.GetArrayElementAtIndex(TuplesProperty.arraySize - 2).FindPropertyRelative("Key");
                NewProp.intValue = (PrevProp.intValue + 1) % MaxValue;
            }
            else
            {
                NewProp.intValue = 0;
            }
            TuplesProperty.serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button(new GUIContent("-"), GUILayout.MaxWidth(25)))
        {
            TuplesProperty.DeleteArrayElementAtIndex(TuplesProperty.arraySize - 1);
            Tuples.RemoveAt(Tuples.Count - 1);
            TuplesProperty.serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel++;

        DrawElements();


        EditorGUI.indentLevel--;
    }

    private void DrawElements()
    {
        float Width = EditorGUIUtility.currentViewWidth - 50;

        string[] Resources = Enum.GetNames(typeof(Production.Type));
        for (int ResourceIndex = 0; ResourceIndex < Tuples.Count; ResourceIndex++) 
        {
            SerializedProperty TupleProperty = Tuples[ResourceIndex];
            SerializedProperty KeyProperty = TupleProperty.FindPropertyRelative("Key");
            SerializedProperty ValueProperty = TupleProperty.FindPropertyRelative("Value");

            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(new GUIContent(Resources[KeyProperty.intValue]), FocusType.Keyboard, GUILayout.MaxWidth(Width / 2.0f)))
            {
                GenericMenu menu = new GenericMenu();

                int CurrentGroupIndex = 1;
                for (int MenuIndex = 0; MenuIndex < Resources.Length; MenuIndex++)
                {
                    menu.AddItem(new GUIContent(Resources[MenuIndex]), MenuIndex == KeyProperty.intValue, Selection =>
                    {
                        KeyProperty.intValue = (int)Selection;
                        KeyProperty.serializedObject.ApplyModifiedProperties();
                    }, MenuIndex);

                    if (MenuIndex == Production.Indices[CurrentGroupIndex] - 1)
                    {
                        menu.AddSeparator("");
                        CurrentGroupIndex++;
                    }
                }
                menu.ShowAsContext();
            }
            ValueProperty.intValue = EditorGUILayout.IntField(
                ValueProperty.intValue,
                GUILayout.MaxWidth(Width / 2.0f)
            );

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.FlexibleSpace();
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