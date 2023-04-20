using System.Collections.Generic;
using UnityEngine;

public abstract class BuildingData {
    public enum Type {
        Default,
        Woodcutter,
        Farm,
        Mine
    }

    public BuildingData() {
        Location = new Location(new Vector2Int(0, 0), new Vector2Int(0, 0));
        Workers = new Worker[GetMaxWorker()];
    }

    public abstract Production GetProduction();

    public abstract Type GetBuildingType();

    public abstract int GetMaxWorker();

    public abstract Production GetCosts();

    public virtual bool IsNeighbourBuildingBlocking() {
        return false;
    }

    public virtual Vector3 GetOffset() {
        return new Vector3(0, 2, 0);
    }

    public virtual Quaternion GetRotation() {
        return Quaternion.Euler(0, 180, 0);
    }

    public Production GetAdjacencyProduction() {
        List<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location);
        Production Production = new();

        if (!TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production>  Bonus))
            return Production;

        foreach (HexagonData Data in NeighbourData) {
            if (MapGenerator.IsBuildingAt(Data.Location) && IsNeighbourBuildingBlocking())
                continue;

            if (Bonus.TryGetValue(Data.Type, out Production AdjacentProduction)) {
                Production += AdjacentProduction;
            }
        }
        return Production;
    }

    public virtual bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus) {
        Bonus = new Dictionary<HexagonConfig.HexagonType, Production>();
        return false;
    }

    public static BuildingData CreateFromType(Type Type) {
        switch (Type) {
            case Type.Mine: return new MineBuilding();
            case Type.Farm: return new FarmBuilding();
            default: return new WoodcutterBuilding();
        }
    }

    public virtual bool CanBeBuildOn(HexagonVisualization Hex) {
        // add additional checks in subclasses!

        if (!Hex)
            return false;

        if (Hex.Data == null)
            return false;

        return !MapGenerator.IsBuildingAt(Hex.Location) && !Hex.Data.bIsMalaised;
    }



    public Location Location;
    public Worker[] Workers;
}
