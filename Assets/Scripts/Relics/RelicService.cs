using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Handles loading and applying of relics
 * Since the relics are GAS-effects, the actual gameplay logic is handled in GAS
 */
public class RelicService : GameService, ISaveableService, IUnlockableService<RelicType>
{
    public Unlockables<RelicType> UnlockableRelics;
    public Dictionary<RelicType, RelicEffect> Relics = new();

    public GameObject ShowRelicButton;

    public int MaxActiveRelics = 3;
    public int CurrentActiveRelics = 0;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UnlockableRelics);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return Unlockables<RelicType>.GetStaticSize(RelicEffect.CategoryCount, GetOverallRelicAmount());
    }

    public int GetOverallRelicAmount()
    {
        return Resources.LoadAll("Relics/", typeof(RelicEffect)).Length;
    }

    public void Reset()
    {
        //todo
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        // will be overwritten by the loading
        UnlockableRelics = new();
        UnlockableRelics.Init(this);
        int Pos = 0;
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UnlockableRelics);
    }

    private void LoadRelicTypes()
    {
        Relics.Clear();

        Object[] Effects = Resources.LoadAll("Relics/", typeof(RelicEffect));
        foreach (var Effect in Effects)
        {
            if (Effect is not RelicEffect)
                continue;

            RelicEffect Relic = Effect as RelicEffect;
            Relics.Add(Relic.Type, Relic);
        }

        ShowRelicButton.SetActive(Relics.Count > 0);
    }

    private void LoadUnlockableRelics()
    {
        LoadUnlockableRelicCategory((int)RelicEffect.CategoryMeadow);
        LoadUnlockableRelicCategory((int)RelicEffect.CategoryDesert);
    }

    private void LoadUnlockableRelicCategory(int Mask)
    {
        SerializedDictionary<RelicType, Unlockables.State> Category = new();
        for (int i = 0; i <= RelicEffect.MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            Category.Add((RelicType)(1 << i), Unlockables.State.Locked);
        }
        UnlockableRelics.AddCategory(Category);

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

            UnlockableRelics = new();
            UnlockableRelics.Init(this);
            _OnInit?.Invoke(this);
        });
    }

    public void OnLoaded()
    {
        _OnInit?.Invoke(this);
    }

    public void SetRelic(RelicType Type, Unlockables.State State, bool bForce = false)
    {
        if (CurrentActiveRelics == MaxActiveRelics && State == Unlockables.State.Active && !bForce)
        {
            ConfirmScreen.Show("Cannot active another relic, reached max capacity!", OnSetRelicConfirm);
            return;
        }

        Unlockables.State OldState = UnlockableRelics[Type];
        UnlockableRelics[Type] = State;

        Game.RunAfterServiceInit((GameplayAbilitySystem GAS) =>
        {
            GameplayAbilityBehaviour Player = GetPlayerBehavior();
            if (Player == null)
                return;

            bool bIsNowActive = (OldState != Unlockables.State.Active || bForce) && 
                State == Unlockables.State.Active;
            bool bIsNowInActive = (OldState == Unlockables.State.Active || bForce) && 
                State != Unlockables.State.Active;

            if (bIsNowActive)
            {
                GAS.TryApplyEffectTo(Player, Relics[Type]);
            }
            if (bIsNowInActive)
            {
                Player.RemoveEffect(Relics[Type]);
            }

            //since we forced the update, we cannot simply increase/decrease
            CurrentActiveRelics = UnlockableRelics.GetCountOfState(Unlockables.State.Active);

            if (OldState == State && !bForce)
                return;

            Relics[Type].BroadcastDiscoveryChanged(State);
            OnRelicDiscoveryChanged.ForEach(_ => _.Invoke(Type, State));
        });
    }

    public void OnLoadUnlockable(RelicType Type, Unlockables.State State)
    {
        // need to force as relics will be loaded into "active" state, but not applied!
        SetRelic(Type, State, true);
    }

    public bool HasIdleSpot()
    {
        if (UnlockableRelics.GetCountOfState(Unlockables.State.Unlocked) == 0)
            return false;

        return CurrentActiveRelics < MaxActiveRelics;
    }

    public void OnSetRelicConfirm()
    {
        // TODO: empty stud for now
    }

    protected override void StopServiceInternal() {}

    bool IUnlockableService<RelicType>.IsInit()
    {
        return IsInit;
    }

    public void InitUnlockables()
    {
        LoadRelicTypes();
        LoadUnlockableRelics();
    }

    public int GetValueAsInt(RelicType Type)
    {
        return HexagonConfig.MaskToInt((int)Type, 32);
    }

    public RelicType GetValueAsT(int Value)
    {
        return (RelicType)HexagonConfig.IntToMask(Value);
    }

    public ActionList<RelicType, Unlockables.State> OnRelicDiscoveryChanged = new();
}
