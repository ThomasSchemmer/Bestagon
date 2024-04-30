using Unity.Collections;
using UnityEngine;

/** Helper class to transfer the cards between scenes. Only contains actually important data 
 * aka no visuals (as this is unnecessary to save and will be regenerated anyway) 
 */
public abstract class CardDTO : ISaveable
{      
    public enum Type
    {
        Building
    }

    public virtual byte[] GetData()
    {
        // save the type to allow for correct creation on load
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)GetCardType());
        return Bytes.ToArray();
    }

    public virtual int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // type
        return 1;
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {        
        // nothing to load/set
    }

    public abstract Type GetCardType();

    public static CardDTO CreateFromCard(Card Card) {

        if (Card is BuildingCard)
            return new BuildingCardDTO(Card);

        return null;
    }

    public static CardDTO CreateForSaveable(NativeArray<byte> Bytes, int Pos)
    {
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);
        Type CardType = (Type)bType;
        if (CardType == Type.Building)
            return new BuildingCardDTO();

        return null;
    }

}
