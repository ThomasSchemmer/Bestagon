using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Service to manage active units in the game, currently used for only tokenized units (Scouts).
 * Since the unit data is independent of any chunk, while UnitVisualization is directly bound and managed by 
 * a chunk, there is no direct link from the UnitData to its visualization
 */
public class Units : UnitProvider<TokenizedUnitData>
{
    public bool TryGetUnitAt(Location Location, out TokenizedUnitData Unit)
    {
        Unit = null;
        foreach (TokenizedUnitData ActiveUnit in Units)
        {
            if (!ActiveUnit.Location.Equals(Location))
                continue;

            Unit = ActiveUnit;
            return true;
        }

        return false;
    }

    public bool IsUnitAt(Location Location)
    {
        return TryGetUnitAt(Location, out TokenizedUnitData Unit);
    }

    public bool TryGetUnitsInChunk(Location ChunkLocation, out List<TokenizedUnitData> UnitsInChunk)
    {
        UnitsInChunk = new();
        foreach (TokenizedUnitData ActiveUnit in Units)
        {
            if (!ActiveUnit.Location.ChunkLocation.Equals(ChunkLocation.ChunkLocation))
                continue;

            UnitsInChunk.Add(ActiveUnit);
        }

        return UnitsInChunk.Count > 0;
    }


    public void AddUnit(TokenizedUnitData Unit)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        Units.Add(Unit);
        _OnUnitCountChanged?.Invoke();

        if (!MapGenerator.TryGetChunkData(Unit.Location, out ChunkData Chunk))
            return;

        if (!Chunk.Visualization)
            return;

        Chunk.Visualization.RefreshTokens();
    }

    public override void KillUnit(TokenizedUnitData Unit)
    {
        base.KillUnit(Unit);

        if (Unit.Visualization != null){
            Destroy(Unit.Visualization);
        }

        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (Units.Count != 0)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        if (Workers.GetTotalWorkerCount() != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
    }

    public int GetIdleScoutCount()
    {
        int Count = 0;
        foreach (var Unit in Units)
        {
            if (Unit is not ScoutData)
                continue;

            if (!Unit.HasRemainingMovement())
                continue;

            Count++;
        }
        return Count;
    }

    public int GetMaxScoutCount()
    {
        int Count = 0;
        foreach (var Unit in Units)
        {
            if (Unit is not ScoutData)
                continue;

            Count++;
        }
        return Count;
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((MapGenerator MapGenerator) =>
        {
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() { }

    public delegate void OnUnitCountChanged();
    public static event OnUnitCountChanged _OnUnitCountChanged;
}
