using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Container for all gameplaytags currently applied to a single object 
 * Contains ids instead of the usual tags
 */
[Serializable]
public class GameplayTagRegularContainer : ISerializationCallbackReceiver
{
    // cannot be serialized!
    public List<Guid> IDs = new();

    [SerializeField, HideInInspector]
    public List<string> _SerializedIDs = new();
    public bool bIsEditing = false;
    public string Name;
    public bool bIsEditable;

    public GameplayTagRegularContainer(string Name, bool bIsEditable = true)
    {
        this.Name = Name;
        this.bIsEditable = bIsEditable;
    }

    public void OnAfterDeserialize()
    {
        // do not clear IDs, as this resets the internal identifier - no matches possible anymore!
        // instead delete no longer existing ones
        for (int i = IDs.Count - 1; i >= 0; i--) { 
            string ParsedID = IDs[i].ToString();
            if (!_SerializedIDs.Contains(ParsedID))
            {
                IDs.RemoveAt(i);
                continue;
            }
            //also don't mark still existing ones for re-adding
            _SerializedIDs.Remove(ParsedID);
        }
        foreach (string Value in _SerializedIDs)
        {
            IDs.Add(Guid.Parse(Value));
        }
    }

    public void OnBeforeSerialize()
    {
        _SerializedIDs.Clear();
        foreach (Guid ID in IDs)
        {
            _SerializedIDs.Add(ID.ToString());
        }
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
