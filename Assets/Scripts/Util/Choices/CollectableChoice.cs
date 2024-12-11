using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Container class that stores infos about the different choices a player can make when collecting relics etc
 */
public class CollectableChoice
{
    public enum ChoiceType
    {
        Building,
        Upgrade,
        Relic,
        Sacrifice,
        Offering
    }

    [SaveableEnum]
    public ChoiceType Type;
}

public class CollectableBuildingChoice : CollectableChoice
{
    public Card GeneratedCard;
    public BuildingConfig.Type BuildingToUnlock = BuildingConfig.Type.DEFAULT;

    public CollectableBuildingChoice(Card GeneratedCard)
    {
        this.Type = ChoiceType.Building;
        this.GeneratedCard = GeneratedCard;
        if (GeneratedCard is not BuildingCard)
            return;

        BuildingCard Building = GeneratedCard as BuildingCard;
        BuildingToUnlock = Building.GetBuildingData().BuildingType;
    }
}

public class CollectableRelicChoice : CollectableChoice
{
    public RelicType RelicToUnlock = RelicType.DEFAULT;
    public CollectableRelicChoice(RelicType RelicType)
    {
        RelicToUnlock = RelicType;
        Type = ChoiceType.Relic;
    }
}

public class CollectableUpgradeChoice : CollectableChoice
{
    public CollectableUpgradeChoice()
    {
        Type = ChoiceType.Upgrade;
    }
}

public class CollectableAltarChoice : CollectableChoice
{
    public CollectableAltarChoice(bool bIsOffering)
    {
        Type = bIsOffering ? ChoiceType.Offering : ChoiceType.Sacrifice;
    }
}