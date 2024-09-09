using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/** Library to handle display functionality for Tags. 
 * Holds the most common shared functionality
 */
public class ContainerDrawerLibrary
{

    protected static bool HandleFolding(ref int FoldDepth, int TokenDepth, bool bIsTokenFolded)
    {
        if (FoldDepth >= 0 && FoldDepth < TokenDepth)
            return true;

        if (FoldDepth >= 0 && FoldDepth == TokenDepth)
        {
            FoldDepth = -1;
        }

        if (bIsTokenFolded)
        {
            FoldDepth = TokenDepth;
        }
        return false;
    }

    protected static bool HandleSearching(Dictionary<int, string> TokenDic, string Token, int Depth)
    {
        string Tag = GetTotalTag(TokenDic, Token, Depth);
        if (bIsSearching && !Tag.Contains(SearchString))
            return true;

        return false;
    }

    protected static void UpdateDic(Dictionary<int, string> TokenDic, string Token, int Depth)
    {
        if (!TokenDic.ContainsKey(Depth))
        {
            TokenDic.Add(Depth, Token);
        }
        TokenDic[Depth] = Token;
    }

    public static void DisplayButtons(SerializedProperty TagsProperty)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Expand All", GUILayout.MaxWidth(100)))
        {
            FoldAll(TagsProperty, false);
        }
        if (GUILayout.Button("Collapse All", GUILayout.MaxWidth(100)))
        {
            FoldAll(TagsProperty, true);
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

    public static string GetTotalTag(Dictionary<int, string> TokenDic, string Token, int Depth)
    {
        string Tag = "";
        for (int i = 0; i < Depth; i++)
        {
            Tag += TokenDic[i] + GameplayTagToken.Divisor;
        }
        Tag += Token;
        return Tag;
    }

    public static bool HasChildElements(int i, SerializedProperty TagsProperty)
    {
        if (TagsProperty.arraySize == 1)
            return true;

        if (i == TagsProperty.arraySize - 1)
            return false;

        SerializedProperty Self = TagsProperty.GetArrayElementAtIndex(i);
        SerializedProperty Other = TagsProperty.GetArrayElementAtIndex(i + 1);
        SerializedProperty SelfDepthProp = Self.FindPropertyRelative("Depth");
        SerializedProperty OtherDepthProp = Other.FindPropertyRelative("Depth");

        return SelfDepthProp.intValue == OtherDepthProp.intValue - 1;
    }

    public static bool HasChildElements(int i, List<GameplayTagToken> Tokens)
    {
        if (Tokens.Count == 1)
            return true;

        if (i == Tokens.Count - 1)
            return false;

        return Tokens[i].Depth == Tokens[i + 1].Depth - 1;
    }

    public static void FoldAll(SerializedProperty TagsProperty, bool bIsFolded)
    {
        for (int i = 0; i < TagsProperty.arraySize; i++)
        {
            SerializedProperty TagProperty = TagsProperty.GetArrayElementAtIndex(i);
            SerializedProperty IsFoldedProp = TagProperty.FindPropertyRelative("bIsFolded");

            IsFoldedProp.boolValue = bIsFolded;
        }
    }


    protected static string GlobalSearchString = "Search Gameplay Tags..";
    protected static string GlobalTagToAddString = "X.Y.Z";

    protected static string SearchString = GlobalSearchString;
    protected static string TagToAddString = GlobalTagToAddString;
    protected static bool bIsSearching = false;
    protected static bool bShowAddTag = false;
}
