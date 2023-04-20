using System.Collections.Generic;

public class WoodcutterBuilding : BuildingData {
    public override Type GetBuildingType() {
        return Type.Woodcutter;
    }

    public override int GetMaxWorker() {
        return 1;
    }

    public override Production GetProduction() {
        return new Production(0, 0, 0, 0);
    }

    public override bool IsNeighbourBuildingBlocking() {
        return true;
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus) {
        Bonus = new Dictionary<HexagonConfig.HexagonType, Production>();
        Bonus.Add(HexagonConfig.HexagonType.Forest, new Production(2, 0, 0, 0));

        return true;
    }

    public override bool CanBeBuildOn(HexagonVisualization Hex) {
        if (!base.CanBeBuildOn(Hex))
            return false;
        
        return Hex.Data.Type == HexagonConfig.HexagonType.Forest || Hex.Data.Type == HexagonConfig.HexagonType.Meadow;
    }

    public override Production GetCosts() {
        return new Production(1, 0, 0, 0);
    }
}