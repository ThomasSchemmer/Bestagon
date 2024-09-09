using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Main access point for all gameplay tag related things
 * Should only be created once per project
 * If you want to add / query tags, do it here
 */
[CreateAssetMenu(fileName = "GameplayTags", menuName = "ScriptableObjects/GameplayTags", order = 1)]
public class GameplayTags : ScriptableObject
{
    public GameplayTagSourceContainer Container = new();
    static GameplayTags GlobalTags = null;

    public void AddTag(string Tag)
    {
        Container.AddTag(Tag);
    }
    
    public int GetParentIndex(int Index)
    {
        GameplayTagToken Child = Container.Tokens[Index];
        // parents have to come before hand, so we can go backwards
        for (int i = Index; i >= 0; i--)
        {
            GameplayTagToken PotentialParent = Container.Tokens[i];
            if (PotentialParent.Depth == Child.Depth - 1)
                return i;
        }
        return -1;
    }

    public bool TryGetParentID(Guid ID, out Guid ParentID)
    {
        ParentID = default;
        int SelfIndex = GetSelfIndex(ID);
        if (SelfIndex == -1)
            return false;

        int ParentIndex = GetParentIndex(SelfIndex);
        if (ParentIndex == -1)
            return false;

        ParentID = Container.Tokens[ParentIndex].ID;
        return true;
    }

    public int GetSelfIndex(Guid ID)
    {
        for (int i = 0; i < Container.Tokens.Count; i++)
        {
            GameplayTagToken PotentialSelf = Container.Tokens[i];
            if (PotentialSelf.ID.Equals(ID))
                return i;
        }
        return -1;
    }

    public bool IsIDFromParent(Guid ChildID, Guid IDToMatch)
    {
        if (ChildID.Equals(IDToMatch))
            return true;

        int LocalIndex = GetSelfIndex(ChildID);
        if (LocalIndex == -1)
            return false;

        int LocalParentIndex = GetParentIndex(LocalIndex);
        if (LocalParentIndex == -1)
            return false;

        GameplayTagToken LocalParent = Container.Tokens[LocalParentIndex];

        return IsIDFromParent(LocalParent.ID, IDToMatch);
    }

    public static GameplayTags Get()
    {
        if (GlobalTags == null)
        {
            GlobalTags = Resources.Load("GAS/GameplayTags") as GameplayTags;
        }
        return GlobalTags;
    }

    public GameplayTags()
    {
        string Test1 = "Tag.Element.Fire";
        string Test2 = "Tag.Element.Cold";
        string Test3 = "Tag.Damage.Fire";

        Container.AddTag(Test1);
        Container.AddTag(Test2);
        Container.AddTag(Test3);
    }

}
