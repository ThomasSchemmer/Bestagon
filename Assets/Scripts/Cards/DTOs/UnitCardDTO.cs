using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using UnityEngine;

/** 
 * Has to save both its type as well as overall size, for re-generating the DTO and reading the memory on load respectively
 * Data layout:
 * 0 - x: CardDTO
 * 0 - 3 (int): overall size of DTO
 * 4: CardDTO base
 * 4 (byte): CardDTO type, aka Unit or Building
 * 5 (byte): Unit type
 * 6 - x: UnitData
 */
public class UnitCardDTO : CardDTO
{
    public UnitData UnitData;

    public UnitCardDTO(Card Card)
    {
        UnitCard UnitCard = Card as UnitCard;
        if (UnitCard == null)
            return;

        UnitData = UnitCard.Unit;
    }

    public UnitCardDTO() {}

    public static UnitCardDTO CreateFromUnitData(UnitData UnitData)
    {
        UnitCardDTO DTO = new();
        DTO.UnitData = UnitData;
        return DTO;
    }

    public override Type GetCardType()
    {
        return Type.Unit;
    }


    public override byte[] GetData()
    {
        // since we have to write the overall size at the first byte, we have to move all base data 
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());

        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Pos, base.GetSize());
        Slice.CopyFrom(base.GetData());
        Pos += base.GetSize();

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UnitData);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        // skip the first "size" byte, as the savegamemanager already has handled it
        int Pos = sizeof(int);

        // now move the base data before we can set the actual data
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Pos, base.GetSize());
        Slice.CopyFrom(base.GetData());

        // initialize the base DTO, aka card type
        NativeArray<byte> BaseBytes = new(base.GetSize(), Allocator.Temp);
        BaseBytes.CopyFrom(Slice.ToArray());
        base.SetData(BaseBytes);
        Pos += base.GetSize();

        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bUnitType);
        UnitData = MeshFactory.CreateDataFromType((UnitData.UnitType)bUnitType);

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UnitData);
    }

    public override bool ShouldLoadWithLoadedSize() { return true; }

    public override int GetSize()
    {
        return GetStaticSize(UnitData.Type);
    }

    public static int GetStaticSize(UnitData.UnitType Type)
    {
        int UnitSize = 0;
        switch (Type)
        {
            case UnitData.UnitType.Scout: UnitSize += ScoutData.GetStaticSize(); break;
            case UnitData.UnitType.Worker: UnitSize += WorkerData.GetStaticSize(); break;
        }
        //overall size
        return GetStaticSize() + UnitSize + sizeof(int);
    }

}
