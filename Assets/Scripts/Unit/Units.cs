using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static Units;

/** 
 * Service to manage active units in the game, currently used for only tokenized units (Scouts).
 * Since the unit data is independent of any chunk, while UnitVisualization is directly bound and managed by 
 * a chunk, there is no direct link from the UnitData to its visualization
 */
public class Units : UnitProvider<TokenizedUnitData>, IQuestRegister<TokenizedUnitData>
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
        _OnUnitCreated.ForEach(_ => _.Invoke(Unit));

        if (!MapGenerator.TryGetChunkVis(Unit.Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.RefreshTokens();
    }

    public override void KillUnit(TokenizedUnitData Unit)
    {
        base.KillUnit(Unit);

        if (Unit.Visualization != null){
            Destroy(Unit.Visualization);
        }
        _OnUnitCountChanged?.Invoke();
        CheckForGameOver();
    }

    public bool HasAnyUnit(UnitData.UnitType UnitType, out TokenizedUnitData FoundUnit)
    {
        foreach (TokenizedUnitData Unit in Units)
        {
            if (Unit.Type != UnitType)
                continue;

            FoundUnit = Unit;
            return true;
        }
        FoundUnit = default;
        return false;
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

    public void InvokeUnitMoved(TokenizedUnitData Unit)
    {
        _OnUnitMoved.ForEach(_ => _.Invoke(Unit));
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
            _OnInit?.Invoke(this);
        });
    }


    protected override void StopServiceInternal() { }

    public delegate void OnUnitCountChanged();
    public static event OnUnitCountChanged _OnUnitCountChanged;
    public static ActionList<TokenizedUnitData> _OnUnitMoved = new();
}
