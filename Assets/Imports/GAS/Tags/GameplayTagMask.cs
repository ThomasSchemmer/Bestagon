using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Inner representation of GameplayTags.
 * Since we are already identifying the tags by GUID, we can just place them in a set
 */
public class GameplayTagMask 
{
    private HashSet<Guid> Mask = new();

    public void Set(Guid ID)
    {
        if (Mask.Contains(ID))
            return;

        Mask.Add(ID);
    }

    public void Remove(Guid ID)
    {
        if (!Mask.Contains(ID))
            return;

        Mask.Remove(ID);
    }

    public void SetIDs(List<Guid> IDs)
    {
        foreach (Guid ID in IDs)
        {
            Set(ID);
        }
    }

    public bool HasID(Guid ID, bool bAllowPartial = false)
    {
        if (Mask.Contains(ID))
            return true;

        if (!bAllowPartial)
            return false;

        if (GameplayTags.Get().TryGetParentID(ID, out Guid ParentID))
            return false;

        return HasID(ParentID, bAllowPartial);
    }

    public void Combine(GameplayTagMask OtherMask)
    {
        Mask.UnionWith(OtherMask.Mask);
    }
}
