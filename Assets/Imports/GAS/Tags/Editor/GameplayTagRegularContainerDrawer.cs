
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/** 
 * Looks up tags from the global source as a dropdown and can store a selection of them
 * Does not support updating tags if they are modified in the source!
 */
[CustomPropertyDrawer(typeof(GameplayTagRegularContainer))]
public class GameplayTagRegularContainerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect Position, SerializedProperty GameplayTagContainerProperty, GUIContent Label)
    {
        EditorGUI.BeginProperty(Position, Label, GameplayTagContainerProperty);

        DisplayRegularContainer(GameplayTagContainerProperty);

        EditorGUI.EndProperty();
    }

    private void DisplayRegularContainer(SerializedProperty GameplayTagContainerProperty)
    {
        //https://youtu.be/1T4S2lFf19s?t=309

        GameplayTags GlobalGameplayTags = GameplayTags.Get();
        if (GlobalGameplayTags == null)
        {
            EditorGUILayout.LabelField("Could not find a global definition of gameplay tags!");
            return;
        }

        bool bIsEditing = DrawEditing(GameplayTagContainerProperty);

        if (bIsEditing)
        {
            SerializedProperty IDsProperty = GameplayTagContainerProperty.FindPropertyRelative("_SerializedIDs");
            ContainerRegularDrawerLibrary.DisplayButtons(GlobalGameplayTags);
            EditorGUILayout.Space(5);
            ContainerRegularDrawerLibrary.DisplayGlobalLookupTags(GlobalGameplayTags, IDsProperty);
        }

        ContainerRegularDrawerLibrary.DisplayRegularTags(GlobalGameplayTags, GameplayTagContainerProperty);

    }

    private bool DrawEditing(SerializedProperty GameplayTagContainerProperty)
    {
        EditorGUILayout.BeginHorizontal();
        SerializedProperty NameProperty = GameplayTagContainerProperty.FindPropertyRelative("Name");
        EditorGUILayout.LabelField(NameProperty.stringValue + ": ", GUILayout.MaxWidth(200));
        GUILayout.FlexibleSpace();

        SerializedProperty EditableProperty = GameplayTagContainerProperty.FindPropertyRelative("bIsEditable");
        SerializedProperty EditingProperty = GameplayTagContainerProperty.FindPropertyRelative("bIsEditing");

        if (EditableProperty.boolValue)
        {
            EditorGUILayout.LabelField("Edit", GUILayout.MaxWidth(25));
            EditingProperty.boolValue = EditorGUILayout.Toggle("", EditingProperty.boolValue, GUILayout.MaxWidth(15));
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.EndHorizontal();

        return EditingProperty.boolValue;
    }

}