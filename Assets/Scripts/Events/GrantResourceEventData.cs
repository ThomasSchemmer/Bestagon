using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GrantResourceEventData : EventData
{

    public GrantResourceEventData()
    {
        Type = EventType.GrantResource;
    }

    public void OnEnable()
    {
        GrantedResource = Production.Empty;
        if (!Game.TryGetService(out BuildingService Unlockables))
            return;

        int Seed = Random.Range(0, 100);
        if (!Unlockables.TryGetRandomResource(Seed, global::Unlockables.State.Unlocked, false, out Production.Type GrantedType))
            return;

        int GrantedAmount = Random.Range(MinAmountGranted, MaxAmountGranted);
        GrantedResource = new Production(GrantedType, GrantedAmount);
    }

    public override string GetDescription()
    {
        return "Grants this resource";
    }

    public override string GetEventName()
    {
        return "Treasure";
    }

    public override GameObject GetEventVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForGrantResourceEffect(this, Parent);
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

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (Hex.Data.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Can only use on visited tiles");
                return false;
            }
        }
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
