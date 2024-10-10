using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorationService : TokenizedEntityProvider<DecorationEntity>
{
    public void CreateNewDecoration(DecorationEntity.DType Type, Location Location)
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        DecorationEntity Decoration = MeshFactory.CreateDataFromType(Type);
        Decoration.SetLocation(Location);
        AddDecoration(Decoration);
    }

    public void AddDecoration(DecorationEntity Decoration)
    {
        Entities.Add(Decoration);
        _OnEntityCreated.ForEach(_ => _.Invoke(Decoration));
    }
    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke(this);
    }
}
