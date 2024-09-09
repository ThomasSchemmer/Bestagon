using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Handles loading and applying of relics
 * Since the relics are GAS-effects, the actual gameplay logic is handled in GAS
 */
public class RelicService : GameService, ISaveableService
{
    public byte[] GetData()
    {
        throw new System.NotImplementedException();
    }

    public int GetSize()
    {
        throw new System.NotImplementedException();
    }

    public void Reset()
    {
        throw new System.NotImplementedException();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        throw new System.NotImplementedException();
    }

    protected GameplayAbilityBehaviour GetPlayerBehavior()
    {
        return transform.parent.GetComponent<GameplayAbilityBehaviour>();
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((GameplayAbilitySystem GAS) =>
        {
            Relic Calligulae = Resources.Load("Relics/Calligulae") as Relic;
            GameplayAbilityBehaviour Player = GetPlayerBehavior();
            GAS.TryApplyEffectTo(Player, Calligulae);
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}
}
