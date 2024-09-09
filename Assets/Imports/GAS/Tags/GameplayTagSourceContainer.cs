using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/** Container for all gameplaytags in a given global setup */
[Serializable]
public class GameplayTagSourceContainer
{
    [HideInInspector]
    public List<GameplayTagToken> Tokens = new();

    /** Iterates over all available tags and inserts the new at the target position (or at the end) 
     * This algorithm is also duplicated in the ..Drawer class due to the serialized property stuff
     * TODO: make a wrapper class to only have this in one place
     */
    public void AddTag(string TagToAdd)
    {
        string[] Tokens = TagToAdd.Split(GameplayTagToken.Divisor);
        int FoundDepth = -1;
        int TokenIndex = 0;
        int FoundIndex = -1;
        for (int i = 0; i < this.Tokens.Count; i++)
        {
            GameplayTagToken Target = this.Tokens[i];

            // a previous one was mismatched
            if (Target.Depth != TokenIndex)
                continue;

            if (!Tokens[TokenIndex].Equals(Target.Token))
                continue;

            TokenIndex++;
            FoundIndex = i;
            FoundDepth = Target.Depth;
        }

        if (FoundIndex == -1)
        {
            FoundIndex = this.Tokens.Count - 1;
            FoundDepth = -1;
        }

        int InsertCount = 0;
        for (int i = TokenIndex; i < Tokens.Length; i++)
        {
            int TargetIndex = FoundIndex + 1 + InsertCount;
            int NewDepth = FoundDepth + 1 + InsertCount;
            GameplayTagToken NewToken = new(Tokens[i], NewDepth, false);
            this.Tokens.Insert(TargetIndex, NewToken);

            InsertCount++;
        }
    }

    public bool TryGetByID(string ID, out GameplayTagToken FoundToken)
    {
        foreach (GameplayTagToken Token in Tokens)
        {
            FoundToken = Token;
            if (Token.ID.Equals(ID))
                return true;
        }

        FoundToken = null;
        return false;
    }
}
