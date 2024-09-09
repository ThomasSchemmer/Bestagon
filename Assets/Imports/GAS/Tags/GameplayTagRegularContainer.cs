using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Container for all gameplaytags currently applied to a single object 
 * Contains ids instead of the usual tags
 */
[Serializable]
public class GameplayTagRegularContainer
{
    public List<string> IDs = new();
    public bool bIsEditing = false;
    public string Name;
    public bool bIsEditable;

    public GameplayTagRegularContainer(string Name, bool bIsEditable = true)
    {
        this.Name = Name;
        this.bIsEditable = bIsEditable;
    }

    public void Verify()
    {
        GameplayTags GameplayTags = GameplayTags.Get();
        for (int i = IDs.Count; i >= 0; i--)
        {
            if (GameplayTags.GetSelfIndex(IDs[i]) != -1)
                continue;

            IDs.Remove(IDs[i]);
        }
    }
}
