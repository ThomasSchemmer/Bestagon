using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : BuildingEffect
{
    public enum Type
    {
        None,
        YieldPerWorker,
        YieldPerAreaAndWorker,
        YieldWorkerPerWorker,
        IncreaseYield
    }

    public Type EffectType = Type.YieldPerWorker;
    public HexagonConfig.HexagonType TileType = HexagonConfig.HexagonType.DEFAULT;
    public BuildingData.Type BuildingType = BuildingData.Type.DEFAULT;
    public Production Production = new Production();
    public int Range = 1;
    public float ProductionIncrease = 1.2f;

    public string GetDescription()
    {
        return "FILL OUT DESCRIPTION GEN";
    }
}