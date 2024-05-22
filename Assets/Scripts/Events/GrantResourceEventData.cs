using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GrantResourceEventData : EventData
{
    public Production GrantedResource;

    public GrantResourceEventData()
    {
        Type = EventType.GrantResource;
    }

    public void OnEnable()
    {
        GrantedResource = Production.Empty;
        if (!Game.TryGetService(out Unlockables Unlockables))
            return;

        if (!Unlockables.TryGetRandomUnlockedResource(out Production.Type GrantedType))
            return;

        int GrantedAmount = UnityEngine.Random.Range(MinAmountGranted, MaxAmountGranted);
        GrantedResource = new Production(GrantedType, GrantedAmount);
    }

    public override string GetDescription()
    {
        return "Grants this resource";
    }

    public override GameObject GetEventVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForGrantResourceEffect(this, Parent);
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, GrantedResource);

        return Bytes.ToArray();
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return EventData.GetStaticSize() + Production.GetStaticSize();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        GrantedResource = Production.Empty;

        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, GrantedResource);
    }

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return true;
    }

    public override void InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddResources(GrantedResource);
    }

    public override bool IsPreviewable()
    {
        return true;
    }

    public override Vector3 GetOffset()
    {
        return Vector3.zero;
    }

    public override Quaternion GetRotation()
    {
        return Quaternion.identity;
    }

    public override bool CanBeInteractedOn(HexagonVisualization Hex)
    {
        return true;
    }

    public override int GetAdjacencyRange()
    {
        return 0;
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        Bonus = GetStandardAdjacencyBonus();
        return true;
    }

    public override bool ShouldShowAdjacency(HexagonVisualization Hex)
    {
        return true;
    }

    public static int MinAmountGranted = 2;
    public static int MaxAmountGranted = 5;
}
