using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/** Version of the library to solely handle regular (aka non-source) tags and their display 
 * Since they are only stored as their IDs, displaying them necessitates looking at the global gameplay tag source and
 * referencing the tags/tokens there
 */
public class ContainerRegularDrawerLibrary : ContainerDrawerLibrary
{
    public static void DisplayRegularTags(GameplayTags GlobalGameplayTags, SerializedProperty RegularTagsContainerProperty)
    {
        EditorGUILayout.BeginVertical();
        SerializedProperty IDsProperty = RegularTagsContainerProperty.FindPropertyRelative("_SerializedIDs");

        for (int i = 0; i < IDsProperty.arraySize; i++)
        {
            SerializedProperty IDProperty = IDsProperty.GetArrayElementAtIndex(i);
            string Tag = GetTotalTagFromID(GlobalGameplayTags, IDProperty.stringValue);
            EditorGUILayout.LabelField(Tag, GUILayout.MaxWidth(200));
        }

        EditorGUILayout.EndVertical();
    }

    private static string GetTotalTagFromID(GameplayTags GlobalGameplayTags, string ID)
    {
        Dictionary<int, string> TokenDic = new();

        for (int i = 0; i < GlobalGameplayTags.Container.Tokens.Count; i++)
        {
            GameplayTagToken Token = GlobalGameplayTags.Container.Tokens[i];

            UpdateDic(TokenDic, Token.Token, Token.Depth);

            if (!Token.Equals(ID))
                continue;

            return GetTotalTag(TokenDic, Token.Token, Token.Depth);
        }
        return "";
    }

    public static void DisplayGlobalLookupTags(GameplayTags GlobalGameplayTags, SerializedProperty IDsProperty)
    {
        int FoldDepth = -1;
        Dictionary<int, string> TokenDic = new();

        EditorGUILayout.BeginVertical();

        for (int i = 0; i < GlobalGameplayTags.Container.Tokens.Count; i++)
        {
            GameplayTagToken Token = GlobalGameplayTags.Container.Tokens[i];

            UpdateDic(TokenDic, Token.Token, Token.Depth);

            if (HandleSearching(TokenDic, Token.Token, Token.Depth))
                continue;

            if (HandleFolding(ref FoldDepth, Token.Depth, Token.bIsFolded))
                continue;

            DisplayGlobalLookupToken(GlobalGameplayTags, IDsProperty, i);
        }

        EditorGUILayout.EndVertical();
    }

    public static void DisplayGlobalLookupToken(GameplayTags GlobalGameplayTags, SerializedProperty IDsProperty, int i)
    {
        EditorGUILayout.BeginHorizontal();

        List<GameplayTagToken> Tokens = GlobalGameplayTags.Container.Tokens;
        GameplayTagToken GlobalToken = Tokens[i];

        EditorGUILayout.Space(GameplayTagTokenDrawer.Indent * GlobalToken.Depth);
        if (HasChildElements(i, Tokens))
        {
            Rect Rect = GUILayoutUtility.GetRect(15, 15);
            bool NewFoldOut = EditorGUI.Foldout(Rect, GlobalToken.bIsFolded, "");
            if (NewFoldOut != GlobalToken.bIsFolded)
            {
                GlobalToken.bIsFolded = !GlobalToken.bIsFolded;
            }
        }
        else
        {
            EditorGUILayout.Space(15);
        }

        bool bIsContained = IsIDContainedInProperty(GlobalGameplayTags, IDsProperty, GlobalToken.ID, true);
        bool bShouldBeContained = EditorGUILayout.Toggle(bIsContained, GUILayout.MaxWidth(15));
        if (bShouldBeContained != bIsContained)
        {
            SetIDInProperty(GlobalToken.ID, IDsProperty, bShouldBeContained);
        }
        EditorGUILayout.LabelField(GlobalToken.Token, GUILayout.MaxWidth(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    protected static bool IsIDContainedInProperty(GameplayTags GlobalGameplayTags, SerializedProperty LocalIDsProperty, Guid TokenID, bool bAllowPartial = false)
    {
        for (int i = 0; i < LocalIDsProperty.arraySize; i++)
        {
            SerializedProperty LocalIDProperty = LocalIDsProperty.GetArrayElementAtIndex(i);
            string LocalID = LocalIDProperty.stringValue;
            Guid LocalGuid = Guid.Parse(LocalID);
            if (TokenID.Equals(LocalID))
                return true;

            if (!bAllowPartial)
                continue;

            if (!GlobalGameplayTags.IsIDFromParent(LocalGuid, TokenID))
                continue;

            return true;
        }

        return false;
    }

    protected static void SetIDInProperty(Guid ID, SerializedProperty IDsProperty, bool bShouldBeContained)
    {
        if (bShouldBeContained)
        {
            int TargetIndex = Mathf.Max(IDsProperty.arraySize - 1, 0);
            IDsProperty.InsertArrayElementAtIndex(TargetIndex);
            SerializedProperty NewIDProperty = IDsProperty.GetArrayElementAtIndex(IDsProperty.arraySize - 1);
            NewIDProperty.stringValue = ID.ToString();
        }
        else
        {
            // iterate backwards to avoid index issues
            for (int i = IDsProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty IDProperty = IDsProperty.GetArrayElementAtIndex(i);
                if (!IDProperty.stringValue.Equals(ID.ToString()))
                    continue;

                IDsProperty.DeleteArrayElementAtIndex(i);
            }
        }
    }

    public static void DisplayButtons(GameplayTags GlobalGameplayTags)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Expand All", GUILayout.MaxWidth(100)))
        {
            FoldAll(GlobalGameplayTags, false);
        }
        if (GUILayout.Button("Collapse All", GUILayout.MaxWidth(100)))
        {
            FoldAll(GlobalGameplayTags, true);
        }

        SearchString = EditorGUILayout.DelayedTextField(SearchString, new GUIStyle("ToolbarSearchTextField"));
        if (SearchString.Equals(string.Empty))
        {
            SearchString = GlobalSearchString;
        }
        bIsSearching = !SearchString.Equals(GlobalSearchString);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    public static void FoldAll(GameplayTags GlobalGameplayTags, bool bIsFolded)
    {
        for (int i = 0; i < GlobalGameplayTags.Container.Tokens.Count; i++)
        {
            GameplayTagToken Token = GlobalGameplayTags.Container.Tokens[i];

            Token.bIsFolded = bIsFolded;
        }
    }
}
