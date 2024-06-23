using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ConvertTileEventData : EventData
{
    public HexagonConfig.HexagonType TargetType;

    public ConvertTileEventData()
    {
        Type = EventType.ConvertTile;
    }

    public void OnEnable()
    {
        if (!Game.TryGetService(out Unlockables Unlockables))
            return;

        if (!Unlockables.TryGetRandomUnlockedTile(out HexagonConfig.HexagonType GrantedType))
            return;

        TargetType = GrantedType;
    }

    public override bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
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

    public override void InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TrySetHexagonData(Hex.Location, HexagonConfig.HexagonHeight.Flat, TargetType))
            return;

        if (!MapGenerator.TryGetChunkData(Hex.Location, out ChunkData Chunk))
            return;

        Chunk.Visualization?.RefreshTokens();
        Hex.UpdateMesh();
        Hex.VisualizeSelection();

        if (!Game.TryGetService(out MiniMap Minimap))
            return;

        Minimap.FillBuffer();
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

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)TargetType);

        return Bytes.ToArray();
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        // HexagonType
        return EventData.GetStaticSize() + 1;
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bTargetType);

        TargetType = (HexagonConfig.HexagonType)bTargetType;
    }

}
