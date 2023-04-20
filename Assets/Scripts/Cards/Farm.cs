using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Farm : Card {
    public override BuildingData GetBuildingData() {
        return new FarmBuilding();
    }

    public override string GetDescription() {
        return "Produces 1F per Turn";
    }

    public override string GetName() {
        return "Farm";
    }

    public override string GetSymbol() {
        return "F";
    }

}
