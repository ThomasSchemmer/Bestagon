using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class UnitCardDTO : CardDTO
{
    UnitData UnitData;

    public UnitCardDTO(Card Card)
    {
        UnitCard UnitCard = Card as UnitCard;
        if (UnitCard == null)
            return;

        UnitData = UnitCard.UnitData;
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
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UnitData);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UnitData);
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return CardDTO.GetStaticSize() + TokenizedUnitData.GetStaticSize();
    }
}
