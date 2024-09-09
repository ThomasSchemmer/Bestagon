using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Inner representation of GameplayTags.
 * Since we are already identifying the tags by GUID string, we can just place them in a set
 */
public class GameplayTagMask 
{
    private HashSet<string> Mask = new();

    public void Set(string ID)
    {
        if (Mask.Contains(ID))
            return;

        Mask.Add(ID);
    }

    public void Clear(string ID)
    {
        if (!Mask.Contains(ID))
            return;

        Mask.Remove(ID);
    }

    public void SetIDs(List<string> IDs)
    {
        foreach (string ID in IDs)
        {
            Set(ID);
        }
    }

    public bool HasID(string ID, bool bAllowPartial = false)
    {
        if (Mask.Contains(ID))
            return true;

        if (!bAllowPartial)
            return false;

        if (GameplayTags.Get().TryGetParentID(ID, out string ParentID))
            return false;

        return HasID(ParentID, bAllowPartial);
    }

    public void Combine(GameplayTagMask OtherMask)
    {
        Mask.UnionWith(OtherMask.Mask);
    }
}
