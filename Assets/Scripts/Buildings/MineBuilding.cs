using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineBuilding : BuildingData {
    public override Type GetBuildingType() {
        return Type.Mine;
    }
    public override int GetMaxWorker() {
        return 1;
    }

    protected override Production _GetProduction() {
        return new Production(0, 0, 2, 0);
    }

    public override Vector3 GetOffset() {
        return new Vector3(-4, 2, -2);
    }
    public override Quaternion GetRotation() {
        return Quaternion.Euler(270, 0, -120);
    }

    public override bool CanBeBuildOn(HexagonVisualization Hex) {
        return base.CanBeBuildOn(Hex) && Hex.Data.Type == HexagonConfig.HexagonType.Mountain;
    }

    public override Production GetCosts() {
        return new Production(2, 1, 1, 0);
    }
}
