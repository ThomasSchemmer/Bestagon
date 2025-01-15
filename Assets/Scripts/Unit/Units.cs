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
public class Units : TokenizedEntityProvider<TokenizedUnitEntity>
{
    public void AddUnit(TokenizedUnitEntity Unit)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        Entities.Add(Unit);
        _OnUnitCountChanged?.Invoke();
        _OnEntityCreated.ForEach(_ => _.Invoke(Unit));

        if (!MapGenerator.TryGetChunkVis(Unit.GetLocations(), out List<ChunkVisualization> ChunkViss))
            return;

        ChunkViss.ForEach(ChunkVis => ChunkVis.RefreshTokens());
    }

    private void CheckForGameOver()
    {
        if (Entities.Count != 0)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        if (Workers.GetTotalWorkerCount() != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
    }

    public void InvokeUnitMoved(TokenizedUnitEntity Unit)
    {
        _OnUnitMoved.ForEach(_ => _.Invoke(Unit));
    }

    public int GetIdleScoutCount()
    {
        int Count = 0;
        foreach (var Unit in Entities)
        {
            if (Unit == null)
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
        foreach (var Unit in Entities)
        {
            if (Unit == null)
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

    public bool HasAnyEntity(UnitEntity.UType UnitType, out TokenizedUnitEntity FoundEntity)
    {
        foreach (TokenizedUnitEntity Entity in Entities)
        {
            if (Entity.EntityType != ScriptableEntity.EType.Unit)
                continue;

            if (Entity.UnitType != UnitType)
                continue;

            FoundEntity = Entity;
            return true;
        }
        FoundEntity = default;
        return false;
    }

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();
        _OnInit?.Invoke(this);
        _OnUnitCountChanged?.Invoke();

    }

    protected override void StopServiceInternal() { }

    public override bool TryCreateNewEntity(int EntityCode, LocationSet Location)
    {
        UnitEntity.UType Code = (UnitEntity.UType)EntityCode;
        if (!Game.TryGetServices(out MeshFactory MeshFactory, out MapGenerator MapGen))
            return false;

        if (!MapGen.TryGetHexagon(Location.GetMainLocation(), out var Hex))
            return false;

        var Unit = MeshFactory.CreateDataFromType(Code);
        var TUnit = Unit as TokenizedUnitEntity;
        if (Unit == null || !Unit.TryInteractWith(Hex) || TUnit == null)
        {
            Destroy(Unit);
            return false;
        }

        TUnit.Init();
        AddUnit(TUnit);

        TUnit.MoveTo(Hex.Location, 0);
        TUnit.Refresh();
        return true;
    }

    public delegate void OnUnitCountChanged();
    public static event OnUnitCountChanged _OnUnitCountChanged;
    public static ActionList<TokenizedUnitEntity> _OnUnitMoved = new();
}
