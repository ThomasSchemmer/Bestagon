using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/** Version of the library to solely handle source tags and their display */
public class ContainerSourceDrawerLibrary : ContainerDrawerLibrary
{
    public static void DisplayAddTag(SerializedProperty GameplayTagContainerProperty)
    {
        EditorGUILayout.BeginVertical();
        bShowAddTag = EditorGUILayout.Foldout(bShowAddTag, "Add new GameplayTag");
        if (bShowAddTag)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.MaxWidth(50));
            TagToAddString = EditorGUILayout.TextField(TagToAddString);
            if (GUILayout.Button("Add Tag", GUILayout.MaxWidth(75)))
            {
                AddTag(GameplayTagContainerProperty);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    public static void AddTag(SerializedProperty GameplayTagContainerProperty)
    {
        if (TagToAddString.Equals(GlobalTagToAddString))
            return;

        SerializedProperty TokensProperty = GameplayTagContainerProperty.FindPropertyRelative("Tokens");

        string[] Tokens = TagToAddString.Split(GameplayTagToken.Divisor);
        int FoundDepth = -1;
        int TokenIndex = 0;
        int FoundIndex = -1;
        for (int i = 0; i < TokensProperty.arraySize; i++)
        {
            SerializedProperty TargetProp = TokensProperty.GetArrayElementAtIndex(i);
            SerializedProperty TargetTokenProp = TargetProp.FindPropertyRelative("Token");
            SerializedProperty TargetDepthProp = TargetProp.FindPropertyRelative("Depth");

            // a previous one was mismatched
            if (TargetDepthProp.intValue != TokenIndex)
                continue;

            if (!Tokens[TokenIndex].Equals(TargetTokenProp.stringValue))
                continue;

            TokenIndex++;
            FoundIndex = i;
            FoundDepth = TargetDepthProp.intValue;
        }

        if (FoundIndex == -1)
        {
            FoundIndex = TokensProperty.arraySize - 1;
            FoundDepth = -1;
        }

        int InsertCount = 0;
        for (int i = TokenIndex; i < Tokens.Length; i++)
        {
            int TargetIndex = FoundIndex + InsertCount;
            TokensProperty.InsertArrayElementAtIndex(TargetIndex);
            TokensProperty.serializedObject.ApplyModifiedProperties();
            SerializedProperty NewTagProp = TokensProperty.GetArrayElementAtIndex(TargetIndex + 1);
            SerializedProperty NewTokenProp = NewTagProp.FindPropertyRelative("Token");
            SerializedProperty NewDepthProp = NewTagProp.FindPropertyRelative("Depth");
            SerializedProperty NewIDProp = NewTagProp.FindPropertyRelative("ID");

            NewTokenProp.stringValue = Tokens[i];
            NewDepthProp.intValue = FoundDepth + 1 + InsertCount;
            NewIDProp.stringValue = Guid.NewGuid().ToString();
            InsertCount++;
        }

        TokensProperty.serializedObject.ApplyModifiedProperties();
    }

    public static void DisplaySourceTags(SerializedProperty TokensProperty)
    {
        EditorGUILayout.BeginVertical();
        int FoldDepth = -1;
        Dictionary<int, string> TokenDic = new();

        for (int i = 0; i < TokensProperty.arraySize; i++)
        {
            SerializedProperty TokenProperty = TokensProperty.GetArrayElementAtIndex(i);
            SerializedProperty IsFoldedProp = TokenProperty.FindPropertyRelative("bIsFolded");
            SerializedProperty TokenProp = TokenProperty.FindPropertyRelative("Token");
            SerializedProperty DepthProp = TokenProperty.FindPropertyRelative("Depth");

            UpdateDic(TokenDic, TokenProp.stringValue, DepthProp.intValue);
            if (HandleSearching(TokenDic, TokenProp.stringValue, DepthProp.intValue))
                continue;

            if (HandleFolding(ref FoldDepth, DepthProp.intValue, IsFoldedProp.boolValue))
                continue;

            //instead of using a library here we move the display into the corresponding drawer
            EditorGUILayout.PropertyField(TokenProperty);
        }
        EditorGUILayout.EndVertical();
    }

}
