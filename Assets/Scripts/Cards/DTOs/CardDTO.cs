using Unity.Collections;
using UnityEngine;

/** Helper class to transfer the cards between scenes. Only contains actually important data 
 * aka no visuals (as this is unnecessary to save and will be regenerated anyway) 
 */
public abstract class CardDTO : ISaveable
{      
    public enum Type
    {
        Building,
        Unit
    }

    public virtual byte[] GetData()
    {
        // save the type to allow for correct creation on load
        // use StaticSize to force only creating the base info struct!
        NativeArray<byte> Bytes = new(GetStaticSize(), Allocator.Temp);
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

    // declare from interface so that children can override
    public virtual bool ShouldLoadWithLoadedSize() { return false; }

    public abstract Type GetCardType();

    public static CardDTO CreateFromCard(Card Card) {

        if (Card is BuildingCard)
            return new BuildingCardDTO(Card);

        if (Card is UnitCard)
            return new UnitCardDTO(Card);

        return null;
    }

    public static CardDTO CreateForSaveable(NativeArray<byte> Bytes, int Pos)
    {
        //skip size info
        Pos += sizeof(int);
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);
        Type CardType = (Type)bType;
        if (CardType == Type.Building)
            return new BuildingCardDTO();
        if (CardType == Type.Unit)
            return new UnitCardDTO();

        return null;
    }

}
