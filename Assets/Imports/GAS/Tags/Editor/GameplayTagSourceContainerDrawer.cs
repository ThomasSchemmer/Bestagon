
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/** Adaptive drawer for source containers
 * supports creation of new tags by inserting tokens into the global truth
 */
[CustomPropertyDrawer(typeof(GameplayTagSourceContainer))]
public class GameplayTagSourceContainerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect Position, SerializedProperty GameplayTagContainerProperty, GUIContent Label)
    {
        EditorGUI.BeginProperty(Position, Label, GameplayTagContainerProperty);

        DisplayGlobalSource(GameplayTagContainerProperty);

        EditorGUI.EndProperty();
    }

    private void DisplayGlobalSource(SerializedProperty GameplayTagContainerProperty)
    {
        SerializedProperty TokensProperty = GameplayTagContainerProperty.FindPropertyRelative("Tokens");

        ContainerSourceDrawerLibrary.DisplayAddTag(GameplayTagContainerProperty);
        EditorGUILayout.Space(5);
        ContainerDrawerLibrary.DisplayButtons(TokensProperty);
        EditorGUILayout.Space(5);
        ContainerSourceDrawerLibrary.DisplaySourceTags(TokensProperty);
    }
}