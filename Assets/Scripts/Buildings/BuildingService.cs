
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class BuildingService : TokenizedEntityProvider<BuildingEntity>
{
    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() { }

    public void DestroyBuildingAt(Location Location)
    {
        if (!TryGetEntityAt(Location, out BuildingEntity Building))
            return;

        // Workers have been killed previously
        Entities.Remove(Building);

        string Text = Building.BuildingType.ToString() + " has been destroyed by the malaise";
        MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);

        _OnBuildingDestroyed.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
        Destroy(Building);
    }

    public override void Reset()
    {
        Entities = new();
    }

    public void AddBuilding(BuildingEntity Building)
    {
        Entities.Add(Building);

        _OnBuildingBuilt.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
    }

    public delegate void OnBuildingsChanged();
    public static OnBuildingsChanged _OnBuildingsChanged;

    public static ActionList<BuildingEntity> _OnBuildingDestroyed = new();
    public static ActionList<BuildingEntity> _OnBuildingBuilt = new();
}
