using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Handles loading and applying of relics
 * Since the relics are GAS-effects, the actual gameplay logic is handled in GAS
 */
public class RelicService : SaveableService, IUnlockableService<RelicType>
{
    [SaveableClass]
    public Unlockables<RelicType> UnlockableRelics;
    public Dictionary<RelicType, RelicEffect> Relics = new();

    public GameObject ShowRelicButton;

    public int MaxActiveRelics = 3;
    public int CurrentActiveRelics = 0;
    
    public int GetOverallRelicAmount()
    {
        return Resources.LoadAll("Relics/", typeof(RelicEffect)).Length;
    }

    public override void Reset()
    {
        base.Reset();
        UnlockableRelics.Reset();
        Relics.Clear();
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
        UnlockableRelics.AddCategory(RelicEffect.CategoryMeadow, RelicEffect.MaxIndex);
        UnlockableRelics.AddCategory(RelicEffect.CategoryDesert, RelicEffect.MaxIndex);
    }

    public GameplayAbilityBehaviour GetPlayerBehavior()
    {
        return transform.parent.GetComponent<GameplayAbilityBehaviour>();
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((GameplayAbilitySystem GAS, SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(SaveableService.SaveGameType.Relics))
                return;

            UnlockableRelics = new();
            UnlockableRelics.Init(this);
            _OnInit?.Invoke(this);
        });
    }

    public override void OnAfterLoaded()
    {
        LoadRelicTypes();
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

    public void OnLoadedUnlockable(RelicType Type, Unlockables.State State)
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
        return (int)Type;
    }

    public RelicType GetValueAsT(int Value)
    {
        return (RelicType)Value;
    }

    public RelicType Combine(RelicType A, RelicType B)
    {
        return A |= B;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnLoadedUnlockables()
    {
        Game.RunAfterServiceInit((RelicService Service) =>
        {
            for (int i = 0; i < Service.UnlockableRelics.GetCategoryCount(); i++)
            {
                var Category = Service.UnlockableRelics.GetCategory(i);
                for (int j = 0; j < Category.Count; j++) {
                    var Key = Category.GetKeyAt(j);
                    Service.OnLoadedUnlockable(Key, Category[Key]);
                }
            }
        });
    }

    public ActionList<RelicType, Unlockables.State> OnRelicDiscoveryChanged = new();
}
