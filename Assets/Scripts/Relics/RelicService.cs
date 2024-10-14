using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Handles loading and applying of relics
 * Since the relics are GAS-effects, the actual gameplay logic is handled in GAS
 */
public class RelicService : GameService, ISaveableService, IUnlockableService
{
    public Unlockables<RelicType> UnlockableRelics;
    public Dictionary<RelicType, RelicEffect> Relics = new();

    public GameObject ShowRelicButton;
    public GameObject RelicIconPrefab;
    public GameObject RelicIconPreviewPrefab;

    public int MaxActiveRelics = 3;
    public int CurrentActiveRelics = 0;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        foreach (RelicType Type in Relics.Keys)
        {
            RelicDTO DTO = RelicDTO.CreateFromRelicEffect(Relics[Type], UnlockableRelics[Type]);
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
        UnlockableRelics = new();
        UnlockableRelics.Init(this);
        foreach (var Type in Relics.Keys)
        {
            RelicDTO DTO = new();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, DTO);
            SetRelic(DTO.Type, DTO.State);
        }
    }

    private void LoadRelics()
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

        SerializedDictionary<RelicType, Unlockables.State> Category = new();
        int Mask = (int)RelicEffect.CategoryMeadow;
        for (int i = 0; i <= RelicEffect.MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            Category.Add((RelicType)(1 << i), Unlockables.State.Locked);
        }
        UnlockableRelics.AddCategory(Category);

        ShowRelicButton.SetActive(Relics.Count > 0);
    }

    public RelicIconScreen CreateRelicIcon(Transform Container, RelicEffect Relic, bool bIsPreview)
    {
        GameObject GO = Instantiate(bIsPreview ? RelicIconPreviewPrefab : RelicIconPrefab);
        RelicIconScreen RelicIcon = GO.GetComponent<RelicIconScreen>();
        RelicIcon.Initialize(Relic, bIsPreview);
        RelicIcon.transform.SetParent(Container, false);
        return RelicIcon;
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

    public void SetRelic(RelicType Type, Unlockables.State State)
    {
        if (CurrentActiveRelics == MaxActiveRelics)
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

            if (OldState != Unlockables.State.Active && State == Unlockables.State.Active)
            {
                GAS.TryApplyEffectTo(Player, Relics[Type]);
                CurrentActiveRelics++;
            }
            if (OldState == Unlockables.State.Active && State != Unlockables.State.Active)
            {
                Player.RemoveEffect(Relics[Type]);
                CurrentActiveRelics--;
            }
        });

        if (OldState == State)
            return;

        Relics[Type].BroadcastDiscoveryChanged(State);
        OnRelicDiscoveryChanged.ForEach(_ => _.Invoke(Type, State));
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

    bool IUnlockableService.IsInit()
    {
        return IsInit;
    }

    public void InitUnlockables()
    {
        LoadRelics();
    }

    public ActionList<RelicType, Unlockables.State> OnRelicDiscoveryChanged = new();
}
