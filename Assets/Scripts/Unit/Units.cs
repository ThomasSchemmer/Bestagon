using System.Collections.Generic;
using UnityEngine;

/** 
 * Service to manage active units in the game, currently used for only tokenized units (Scouts).
 * Since the unit data is independent of any chunk, while UnitVisualization is directly bound and managed by 
 * a chunk, there is no direct link from the UnitData to its visualization
 */
public class Units : GameService
{
    // todo: save
    public List<TokenizedUnitData> ActiveUnits = new();

    public bool TryGetUnitAt(Location Location, out TokenizedUnitData Unit)
    {
        Unit = null;
        foreach (TokenizedUnitData ActiveUnit in ActiveUnits)
        {
            if (!ActiveUnit.Location.Equals(Location))
                continue;

            Unit = ActiveUnit;
            return true;
        }

        return false;
    }

    public bool TryGetUnitsInChunk(Location ChunkLocation, out List<TokenizedUnitData> Units)
    {
        Units = new();
        foreach (TokenizedUnitData ActiveUnit in ActiveUnits)
        {
            if (!ActiveUnit.Location.ChunkLocation.Equals(ChunkLocation.ChunkLocation))
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

    public void KillUnit(TokenizedUnitData Unit)
    {
        ActiveUnits.Remove(Unit);

        if (Unit.Visualization != null){
            Destroy(Unit.Visualization);
        }

        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (ActiveUnits.Count != 0)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        if (Workers.ActiveWorkers.Count != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
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
        new Location(new Vector2Int(0, 0), new Vector2Int(4, 4)),
    };
}
