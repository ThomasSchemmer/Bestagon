
using Unity.Collections;
using UnityEngine;

/** Saves a BuildingData Card as a lightweight struct 
 * Could be replaced by buildingdata directly, but might have added stuff later
 */
public class BuildingCardDTO : CardDTO
{
    [SaveableClass]
    public BuildingEntity BuildingData;

    public BuildingCardDTO(Card Card)
    {
        BuildingCard BuildingCard = Card as BuildingCard;
        if (BuildingCard == null)
            return;

        BuildingData = BuildingCard.GetDTOData();
    }

    public BuildingCardDTO() {
        // create an empty one, will be overwritten anyway
        BuildingData = ScriptableObject.CreateInstance<BuildingEntity>();
    }

    public override Type GetCardType()
    {
        return Type.Building;
    }

    public static BuildingCardDTO CreateFromBuildingData(BuildingEntity Data)
    {
        BuildingCardDTO DTO = new();
        DTO.BuildingData = Data;

        //todo: possible infinity bug
        int AdditionalUsability = (int)AttributeSet.Get()[AttributeType.CardNewUsability].CurrentValue;
        DTO.BuildingData.CurrentUsages += AdditionalUsability;
        DTO.BuildingData.MaxUsages += AdditionalUsability;
        DTO.BuildingData.UpgradeMaxUsages += AdditionalUsability;

        return DTO;
    }

    public static BuildingCardDTO CreateFromBuildingData()
    {
        BuildingCardDTO DTO = new();
        return DTO;
    }

    public override bool ShouldBeDeleted()
    {
        return false;
    }
}
