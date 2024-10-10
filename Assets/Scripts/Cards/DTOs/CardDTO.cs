using Unity.Collections;
using UnityEngine;

/** Helper class to transfer the cards between scenes. Only contains actually important data 
 * aka no visuals (as this is unnecessary to save and will be regenerated anyway) 
 */
public abstract class CardDTO : ISaveableData
{      
    public enum Type
    {
        Building,
        Event
    }

    public int PinnedIndex = -1;

    public virtual byte[] GetData()
    {
        // save the type to allow for correct creation on load
        // use StaticSize to force only creating the base info struct!
        NativeArray<byte> Bytes = new(GetStaticSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)GetCardType());
        Pos = SaveGameManager.AddInt(Bytes, Pos, PinnedIndex);
        return Bytes.ToArray();
    }

    public virtual int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // type + pinned index
        return sizeof(byte) + sizeof(int);
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        // skip card type, its already checked in @CreateForSaveable
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bCardType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out PinnedIndex);
    }

    // declare from interface so that children can override
    public virtual bool ShouldLoadWithLoadedSize() { return false; }

    public abstract Type GetCardType();

    public static CardDTO CreateFromCard(Card Card) {

        CardDTO DTO = null;
        if (Card is BuildingCard)
            DTO = new BuildingCardDTO(Card);

        if (Card is EventCard)
            DTO = new EventCardDTO(Card);

        if (DTO == null)
        {
            throw new System.Exception("Could not create DTO for Card");
        }

        DTO.PinnedIndex = Card.GetPinnedIndex();

        return DTO;
    }

    public static CardDTO CreateForSaveable(NativeArray<byte> Bytes, int Pos)
    {
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);
        Type CardType = (Type)bType;

        switch (CardType)
        {
            case Type.Building: return new BuildingCardDTO(); 
            case Type.Event: return new EventCardDTO(); 
            default: return null;
        }
    }

}
