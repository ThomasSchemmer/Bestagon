using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class FarmBuilding : BuildingData
{
    public override Type GetBuildingType() {
        return Type.Farm;
    }

    protected override Production _GetProduction() {
        return new Production(0, 0, 0, 2);
    }
    public override int GetMaxWorker() {
        return 1;
    }

    public override bool CanBeBuildOn(HexagonVisualization Hex) {
        return base.CanBeBuildOn(Hex) && Hex.Data.Type == HexagonConfig.HexagonType.Meadow;
    }

    public override Production GetCosts() {
        return new Production(2, 0, 0, 0);
    }
}
