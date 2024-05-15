using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveMalaiseEventData : EventData
{
    public RemoveMalaiseEventData()
    {
        Type = EventType.RemoveMalaise;
    }

    public override bool CanBeInteractedOn(HexagonVisualization Hex)
    {
        return Hex.Data.IsMalaised();
    }

    public override int GetAdjacencyRange()
    {
        return 0;
    }

    public override string GetDescription()
    {
        return "Removes malaise from one tile";
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

        if (Hex.Chunk.Visualization == null)
            return;

        if (Hex.Chunk.Visualization.MalaiseVisualization == null)
            return;

        Hex.Chunk.Visualization.MalaiseVisualization.GenerateMesh();
    }

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return Hex.Data.IsMalaised();
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