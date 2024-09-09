using System;
using UnityEngine;

/** Implicit version of token hierarchy, derived from a combined tags
 * Unlike a normal node it does not know its children or parent and its Depth is given programatically
 * Using a full string instead would make changes to the Tag hard to propagate down after serialization
 * Was using a node based system before, but Unity does not like serializing a self-referential class, so a conversion is necessary
 * Advantages:
 * - No serialization necessary
 * - Parent/child relation is implicit with depth value (so no indexing necessary)
 * Disadvantages:
 * - Need to iterate over all strings all the time. Indexing is not feasible as one would have to update them anyway
 * - Inserting is harder compared to Node-based
 */
[Serializable]
public class GameplayTagToken : ISerializationCallbackReceiver
{

    // internal ID
    public Guid ID;
    [SerializeField, HideInInspector]
    public string _SerializedID;
    [SerializeField]
    public string Token;
    [SerializeField]
    public int Depth;
    [SerializeField]
    public bool bIsFolded;

    public GameplayTagToken(string Token, int Depth, bool bIsFolded)
    {
        ID = Guid.NewGuid();
        _SerializedID = ID.ToString();
        this.Token = Token;
        this.Depth = Depth;
        this.bIsFolded = bIsFolded;
    }

    public override string ToString()
    {
        return Token;
    }

    public override bool Equals(object obj)
    {
        if (obj is GameplayTagToken)
        {
            GameplayTagToken Other = (GameplayTagToken)obj;
            return Token.Equals(Other.Token) && Depth == Other.Depth && bIsFolded == Other.bIsFolded;
        }
        if (obj is string)
        {
            string Other = (string)obj;
            return ID.ToString().Equals(Other);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Token.GetHashCode();
    }



    public void OnAfterDeserialize()
    {
        ID = Guid.Parse(_SerializedID);
    }

    public void OnBeforeSerialize()
    {
        _SerializedID = ID.ToString();
    }


    public static char Divisor = '.';
}
