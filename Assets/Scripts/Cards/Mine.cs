
public class Mine : Card
{
    public override string GetName() {
        return "Mine";
    }

    public override string GetDescription() {
        return "Produces 1I per Turn";
    }

    public override string GetSymbol() {
        return "M";
    }

    public override BuildingData GetBuildingData() {
        return new MineBuilding();
    }
}
