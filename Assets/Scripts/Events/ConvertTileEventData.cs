using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ConvertTileEventData : EventData
{

    public ConvertTileEventData()
    {
        Type = EventType.ConvertTile;
    }

    public void OnEnable()
    {
        Game.RunAfterServiceInit((BuildingService BuildingService) =>
        {
            if (!BuildingService.TryGetRandomUnlockedTile(out HexagonConfig.HexagonType GrantedType))
                return;

            TargetHexType = GrantedType;
        });
    }

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (Hex.Data.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Can only swap scouted tiles");
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
        return "Converts any tile to";
    }

    public override string GetEventName()
    {
        return "Conversion";
    }

    public override GameObject GetEventVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForConvertTileEvent(this, Parent);
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
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (!MapGenerator.TrySetHexagonData(Hex.Location, HexagonConfig.HexagonHeight.Flat, TargetHexType))
            return false;
        
        if (!MapGenerator.TryGetChunkVis(Hex.Location, out ChunkVisualization ChunkVis))
            return false;

        ChunkVis?.RefreshTokens();
        Hex.UpdateMesh();
        Hex.VisualizeSelection();

        if (!Game.TryGetService(out MiniMap Minimap))
            return false;

        Minimap.FillBuffer();
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
