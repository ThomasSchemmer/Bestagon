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
    public Dictionary<RelicType, RelicDiscovery> RelicStatus = new();
    public Dictionary<RelicType, RelicEffect> Relics = new();

    public GameObject ShowRelicButton;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        foreach (RelicType Type in Relics.Keys)
        {
            RelicDTO DTO = RelicDTO.CreateFromRelicEffect(Relics[Type], RelicStatus[Type]);
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, DTO);
        }

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return Resources.LoadAll("Relics/", typeof(RelicEffect)).Length * RelicDTO.GetStaticSize();
    }

    public void Reset()
    {
        //todo
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        LoadRelics();
        foreach (var Type in Relics.Keys)
        {
            RelicDTO DTO = new();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, DTO);
            RelicStatus[DTO.Type] = DTO.Disc;
        }
    }

    private void LoadRelics()
    {
        Relics.Clear();
        RelicStatus.Clear();

        Object[] Effects = Resources.LoadAll("Relics/", typeof(RelicEffect));
        foreach (var Effect in Effects)
        {
            if (Effect is not RelicEffect)
                continue;

            RelicEffect Relic = Effect as RelicEffect;
            Relics.Add(Relic.Type, Relic);
            RelicStatus.Add(Relic.Type, RelicDiscovery.Unknown);
        }

        ShowRelicButton.SetActive(Relics.Count > 0);
    }

    public GameplayAbilityBehaviour GetPlayerBehavior()
    {
        return transform.parent.GetComponent<GameplayAbilityBehaviour>();
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((GameplayAbilitySystem GAS, SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(ISaveableService.SaveGameType.Relics))
                return;

            LoadRelics();
            //SetRelic(RelicType.Calligulae, RelicDiscovery.Active);
            //SetRelic(RelicType.WoodenMallet, RelicDiscovery.Active);
            //SetRelic(RelicType.Cradle, RelicDiscovery.Active);
            _OnInit?.Invoke(this);
        });
    }

    public void Load()
    {
        _OnInit?.Invoke(this);
    }

    public void SetRelic(RelicType Type, RelicDiscovery Discovery)
    {
        if (!RelicStatus.ContainsKey(Type))
            return;

        RelicStatus[Type] = Discovery;
        Game.RunAfterServiceInit((GameplayAbilitySystem GAS) =>
        {
            GameplayAbilityBehaviour Player = GetPlayerBehavior();
            if (Player == null)
                return;

            if (Discovery == RelicDiscovery.Active)
            {
                GAS.TryApplyEffectTo(Player, Relics[Type]);
            }
            if (Discovery == RelicDiscovery.Discovered)
            {
                Player.RemoveEffect(Relics[Type]);
            }
        });
        Relics[Type].BroadcastDiscoveryChanged(Discovery);
    }

    protected override void StopServiceInternal() {}
}
