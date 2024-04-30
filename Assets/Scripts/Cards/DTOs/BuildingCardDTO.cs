
using Unity.Collections;
using UnityEngine;

/** Saves a BuildingData Card as a lightweight struct 
 * Could be replaced by buildingdata directly, but might have added stuff later
 */
public class BuildingCardDTO : CardDTO
{
    public BuildingData BuildingData;

    public BuildingCardDTO(Card Card)
    {
        BuildingCard BuildingCard = Card as BuildingCard;
        if (BuildingCard == null)
            return;

        BuildingData = BuildingCard.GetDTOData();
    }

    public BuildingCardDTO() {
        BuildingData = ScriptableObject.CreateInstance<BuildingData>();
    }

    public override Type GetCardType()
    {
        return Type.Building;
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, BuildingData);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, BuildingData);
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return CardDTO.GetStaticSize() + BuildingData.GetStaticSize();
    }

    public static BuildingCardDTO CreateFromBuildingData(BuildingData Data)
    {
        BuildingCardDTO DTO = new();
        DTO.BuildingData = Data;
        return DTO;
    }

    public static BuildingCardDTO CreateFromBuildingData()
    {
        BuildingCardDTO DTO = new();
        DTO.BuildingData = ScriptableObject.CreateInstance<BuildingData>();;
        return DTO;
    }
}
