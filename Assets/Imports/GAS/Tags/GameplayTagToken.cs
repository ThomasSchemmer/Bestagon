using System;

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
public class GameplayTagToken
{
    /** Ugly string id, should be int or something, but hard to make sure its unique */
    public string ID;
    public string Token;
    public int Depth;
    public bool bIsFolded;

    public GameplayTagToken(string Token, int Depth, bool bIsFolded)
    {
        ID = Guid.NewGuid().ToString();
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
        if (obj is not GameplayTagToken) 
            return false;

        GameplayTagToken Other = (GameplayTagToken)obj;
        return Token.Equals(Other.Token) && Depth == Other.Depth && bIsFolded == Other.bIsFolded;
    }

    public override int GetHashCode()
    {
        return Token.GetHashCode();
    }

    public static char Divisor = '.';
}
