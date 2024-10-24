using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/** 
 * Allows for switching different card collections while playing
 * Contains a sets of @CardDTOs that are moved around and converted to cards when necessary
 */
public class CardGroupManager : GameService, ISaveableService
{
    public CardGroupsScreen CardGroupsScreen;
    public CardContainerUI CardContainer;

    private List<CardGroup> CardGroups = new List<CardGroup>();

    private int ActiveGroupIndex = -1;

    public void SwitchTo(int NextGroup)
    {
        if (ActiveGroupIndex != -1)
        {
            GetActiveCardGroup().RemoveCards();
        }

        ActiveGroupIndex = NextGroup;
        if (ActiveGroupIndex != -1)
        {
            GetActiveCardGroup().InsertCards();
        }
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((CardDeck CardDeck, CardHand CardHand) =>
        {
            Game.RunAfterServicesInit((DiscardDeck DiscardDeck, CardStash CardStash) =>
            {
                if (!Game.TryGetService(out SaveGameManager Manager))
                    return;

                if (Manager.HasDataFor(ISaveableService.SaveGameType.CardGroups))
                    return;

                CreateBaseGroups();
                _OnInit?.Invoke(this);
            });
        });
    }

    protected override void StopServiceInternal()
    {

    }

    public void Reset()
    {
        // delete groups
    }

    public void OnLoaded() {
        CardGroupsScreen.CreateCardGroupScreens(this, ActiveGroupIndex != -1 ? ActiveGroupIndex : 0);
        _OnInit?.Invoke(this);
    }

    public int GetDisplayedCardCount()
    {
        if (CardContainer == null)
            return 0;

        return CardContainer.Cards.Count;
    }

    public CardGroup GetActiveCardGroup()
    {
        if (ActiveGroupIndex == -1)
            return null;

        return CardGroups[ActiveGroupIndex];
    }

    public int GetActiveIndex()
    {
        return ActiveGroupIndex;
    }

    public int GetCardGroupCount()
    {
        if (CardGroups == null) 
            return 0;

        return CardGroups.Count;
    }

    public CardGroup GetCardGroup(int Index)
    {
        return CardGroups[Index];
    }

    public bool TryGetScreenForGroup(int GroupIndex, out CardGroupScreen FoundScreen)
    {
        FoundScreen = default;
        for (int i = 0; i < CardGroupsScreen.GetCardGroupScreenCount(); i++)
        {
            var Screen = CardGroupsScreen.GetCardGroupScreen(i);
            if (Screen.GetCardGroupTarget().GroupIndex != GroupIndex)
                continue;

            FoundScreen = Screen;
            return true;
        }

        return false;
    }

    private void CreateBaseGroups()
    {
        for (int i = 0; i < GroupCount; i++) {
            CardGroups.Add(new CardGroup(i));
        }

        Game.RunAfterServicesInit((CardFactory CardFactory, CardHand CardHand) =>
        {
            CardFactory.CreateCard(UnitEntity.UType.Scout, 0, null, AddScoutCard);
            CardFactory.CreateCard(BuildingConfig.Type.Woodcutter, 0, null, AddCard);
            CardFactory.CreateCard(BuildingConfig.Type.ForagersHut, 0, null, AddCard);
            CardFactory.CreateCard(BuildingConfig.Type.Claypit, 0, null, AddCard);
            CardFactory.CreateCard(BuildingConfig.Type.Hut, 0, null, AddCard);

            // move cards to the collections, and fill the base hand
            SwitchTo(0);
            CardGroupsScreen.CreateCardGroupScreens(this, ActiveGroupIndex);
        });
    }

    private void AddCard(Card Card)
    {
        CardDTO DTO = CardDTO.CreateFromCard(Card);
        DestroyImmediate(Card.gameObject);
        CardGroups[0].CardDeck.Add(DTO);
    }

    private void AddScoutCard(Card Card)
    {
        EventCard ECard = Card as EventCard;
        GrantUnitEventData EData = ECard.EventData as GrantUnitEventData;
        EData.bIsTemporary = false;
        AddCard(Card);
    }

    public int GetSize()
    {
        // overall size, group count and active group
        int Count = sizeof(int) * 3;
        foreach (var Group in CardGroups)
        {
            Count += Group.GetSize();
        }
        return Count;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, ActiveGroupIndex);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CardGroups.Count);

        foreach (var Group in CardGroups)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Group);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        SwitchTo(-1);
        CardGroups = new();

        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ActiveGroupIndex);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            // index will be overwritten on load
            CardGroup Group = new(-1);
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Group);
            CardGroups.Add(Group);
        }
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public void OnBeforeSaved()
    {
        SwitchTo(-1);
    }

    public static int GroupCount = 4;
}
