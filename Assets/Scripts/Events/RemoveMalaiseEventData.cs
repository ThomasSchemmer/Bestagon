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

    public override bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (Hex.Data.MalaisedState == HexagonData.MalaiseState.None)
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

    public override void InteractWith(HexagonVisualization Hex)
    {
        Hex.Data.MalaisedState = HexagonData.MalaiseState.None;
        Hex.Chunk.Malaise.UnmarkToMalaise(Hex.Location);
        Hex.VisualizeSelection();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        MapGenerator.bAreMalaiseDTOsDirty = true;
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

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        //since this eventdata does need more info, we can just reuse the upper class
        return EventData.GetStaticSize();
    }
}
