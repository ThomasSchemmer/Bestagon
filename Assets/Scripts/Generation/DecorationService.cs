using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorationService : TokenizedEntityProvider<DecorationEntity>
{
    public LocalizedGameplayEffect MalaiseImmunityEffect;
    public LocalizedGameplayEffect ProductionIncreaseEffect;

    public void CreateNewDecoration(DecorationEntity.DType Type, Location Location)
    {
        if (Type == DecorationEntity.DType.DEFAULT)
            return;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        DecorationEntity Decoration = MeshFactory.CreateDataFromType(Type);
        Decoration.SetLocation(Location.ToSet());
        AddDecoration(Decoration);
    }

    public void AddDecoration(DecorationEntity Decoration)
    {
        Entities.Add(Decoration);
        _OnEntityCreated.ForEach(_ => _.Invoke(Decoration));
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetChunkVis(Decoration.GetLocations(), out var Viss))
            return;

        Viss.ForEach(Vis => Vis.RefreshTokens());
    }
    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();

        if (!Game.TryGetService(out GameplayAbilitySystem GAS))
            return;

        // these are handled as always-active global effects with only local actual application
        // see @LocalizedGameplayEffect
        GAS.TryApplyEffectTo(GetPlayerGA(), MalaiseImmunityEffect);
        GAS.TryApplyEffectTo(GetPlayerGA(), ProductionIncreaseEffect);

        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal()
    {
        base.StopServiceInternal();

        GetPlayerGA().RemoveEffect(MalaiseImmunityEffect);
        GetPlayerGA().RemoveEffect(ProductionIncreaseEffect);
    }

    private GameplayAbilityBehaviour GetPlayerGA()
    {
        GameObject PlayerGO = GameObject.Find("Player");
        GameplayAbilityBehaviour Player = PlayerGO.GetComponent<GameplayAbilityBehaviour>();
        return Player;
    }

    public override bool TryCreateNewEntity(int EntityCode, LocationSet Location)
    {
        var Code = (DecorationEntity.DType)EntityCode;
        CreateNewDecoration(Code, Location.GetMainLocation());
        return true;
    }
}
