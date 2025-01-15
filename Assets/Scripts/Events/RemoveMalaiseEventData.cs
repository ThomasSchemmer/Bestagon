using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class RemoveMalaiseEventData : EventData
{
    public RemoveMalaiseEventData()
    {
        Type = EventType.RemoveMalaise;
    }

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (!Hex.Data.IsAnyMalaised())
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot remove malaise - Tile is not afflicted");
            }
            return false;
        }

        return true;
    }

    public override int GetAdjacencyRange()
    {
        return 0;
    }

    public override string GetDescription()
    {
        return "Removes malaise from one tile";
    }

    public override string GetEventName()
    {
        return "Cleanse";
    }

    public override GameObject GetEventVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForRemoveMalaiseEffect(this, Parent);
    }

    public override Vector3 GetOffset()
    {
        return Vector3.zero;
    }

    public override Quaternion GetRotation()
    {
        return Quaternion.identity;
    }

    public override bool InteractWith(HexagonVisualization Hex)
    {
        Hex.Data.RemoveMalaise();
        Hex.Chunk.Malaise.UnmarkToMalaise(Hex.Location);
        Hex.VisualizeSelection();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        MapGenerator.bAreMalaiseDTOsDirty = true;
        return true;
    }

    public override bool IsPreviewable()
    {
        return true;
    }

    public override bool ShouldShowAdjacency(HexagonVisualization Hex)
    {
        return false;
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        Bonus = GetStandardAdjacencyBonus();
        return true;
    }

}
