using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameService;

public class Units : GameService
{
    public List<UnitData> ActiveUnits = new();

    public bool TryGetUnitsAt(Location Location, out List<UnitData> Units, bool bIsChunkLocation = false)
    {
        Units = new List<UnitData>();
        foreach (UnitData ActiveUnit in ActiveUnits)
        {
            if (!bIsChunkLocation && !ActiveUnit.Location.Equals(Location))
                continue;

            if (bIsChunkLocation && !ActiveUnit.Location.ChunkLocation.Equals(Location.ChunkLocation))
                continue;

            Units.Add(ActiveUnit);
        }

        return Units.Count > 0;
    }

    public void RefreshUnits()
    {
        foreach(UnitData ActiveUnit in ActiveUnits)
        {
            ActiveUnit.Refresh();
        }
    }

    public Production GetUnitCosts()
    {
        Production Costs = new();
        foreach (UnitData ActiveUnit in ActiveUnits)
        {
            Costs += ActiveUnit.GetCosts();
        }
        return Costs;
    }

    public void KillUnit(UnitData Unit)
    {
        ActiveUnits.Remove(Unit);
        if (Unit.Visualization == null)
            return;

        Destroy(Unit.Visualization);
    }

    public int HandleStarvation(int Food)
    {
        if (Food >= 0)
            return 0;

        int AmountToStarve = Mathf.Min(Mathf.Abs(Food), ActiveUnits.Count);

        for (int i = 0; i < AmountToStarve; i++)
        {
            KillUnit(ActiveUnits[0]);
        }

        MessageSystem.CreateMessage(Message.Type.Warning, AmountToStarve + " units died of starvation!");
        return AmountToStarve;
    }


    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((MapGenerator MapGenerator) =>
        {
            for (int i = 0; i < ScoutStartLocations.Count; i++)
            {
                ScoutData Scout = new ScoutData();
                Scout.SetName("Scout " + i);
                Scout.MoveTo(ScoutStartLocations[i], 0);
                ActiveUnits.Add(Scout);

                if (!MapGenerator.TryGetChunkData(Scout.Location, out ChunkData Chunk))
                    continue;

                if (!Chunk.Visualization)
                    continue;

                Chunk.Visualization.RefreshTokens();
            }
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() { }

    public static List<Location> ScoutStartLocations = new() {
        new Location(new Vector2Int(0, 0), new Vector2Int(0, 3)),
    };
}
