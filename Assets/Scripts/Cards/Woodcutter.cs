using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Woodcutter : Card {
    public override string GetName() {
        return "WoodCutter";
    }

    public override string GetDescription() {
        return "Produces 1W per Turn and adjacent Forest tile";
    }

    public override string GetSymbol() {
        return "W";
    }

    public override BuildingData GetBuildingData() {
        return new WoodcutterBuilding();
    }
}
