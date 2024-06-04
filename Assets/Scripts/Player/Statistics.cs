using Unity.Collections;

public class Statistics : GameService, ISaveable
{
    private int BuildingsBuilt = 0;
    private int HexesMoved = 0;
    private int UnitsCreated = 0;
    private int ResourcesCollected = 0;

    private void CountResources(Production Production)
    {
        foreach (var Tuple in Production.GetTuples())
        {
            ResourcesCollected += Tuple.Value;
        }
    }

    private void CountUnit(UnitData UnitData)
    {
        UnitsCreated++;
    }

    private void CountMoves(int Moves)
    {
        HexesMoved += Moves;
    }

    private void CountBuilding(BuildingData BuildingData)
    {
        BuildingsBuilt++;
    }



    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingsBuilt);
        Pos = SaveGameManager.AddInt(Bytes, Pos, HexesMoved);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UnitsCreated);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ResourcesCollected);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public int GetStaticSize()
    {
        return sizeof(int) * 4;
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BuildingsBuilt);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out HexesMoved);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UnitsCreated);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ResourcesCollected);
    }

    protected override void StartServiceInternal()
    {
        Stockpile._OnResourcesCollected += CountResources;
        UnitData._OnUnitCreated += CountUnit;
        TokenizedUnitData._OnMovement += CountMoves;
        BuildingData._OnBuildingBuilt += CountBuilding;
    }

    protected override void StopServiceInternal() {}

    private void OnDestroy()
    {
        Stockpile._OnResourcesCollected -= CountResources;
        UnitData._OnUnitCreated -= CountUnit;
        TokenizedUnitData._OnMovement -= CountMoves;
        BuildingData._OnBuildingBuilt -= CountBuilding;
    }
}
