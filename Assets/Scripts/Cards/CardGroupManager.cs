using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/** 
 * Allows for switching different card collections while playing
 * Contains a sets of @CardDTOs that are moved around and converted to cards when necessary
 */
public class CardGroupManager : SaveableService
{
    public CardGroupsScreen CardGroupsScreen;
    public CardContainerUI CardContainer;

    [SaveableList]
    private List<CardGroup> CardGroups = new List<CardGroup>();

    [SaveableBaseType]
    private int ActiveGroupIndex = -1;

    private int PreSaveActiveGroupIndex = -1;

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

                if (Manager.HasDataFor(SaveableService.SaveGameType.CardGroups))
                    return;

                CreateBaseGroups();
                _OnInit?.Invoke(this);
            });
        });
    }

    protected override void StopServiceInternal() {}

    public override void OnBeforeSaved(bool bShouldReset)
    {
        // since none of the actively created cards are stored in the groups as DTO,
        // we need to forcibly move them first
        PreSaveActiveGroupIndex = ActiveGroupIndex;
        SwitchTo(-1);
        if (bShouldReset)
        {
            CleanUpCards();
        }
    }

    public override void OnAfterSaved()
    {
        SwitchTo(PreSaveActiveGroupIndex);
    }

    public override void Reset()
    {
        base.Reset();
        CardGroups.Clear();
        ActiveGroupIndex = -1;
    }

    private void CleanUpCards()
    {
        foreach (var CardGroup in CardGroups)
        {
            CardGroup.CleanUpCards();
        }
    }

    public override void OnAfterLoaded() {
        ApplyPinnedPosition();
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

    private void ApplyPinnedPosition()
    {
        foreach (var CardGroup in CardGroups)
        {
            CardGroup.ApplyPinnedPosition();
        }
    }

    private void CreateBaseGroups()
    {
        for (int i = 0; i < GroupCount; i++) {
            CardGroups.Add(new CardGroup(i));
        }

        Game.RunAfterServicesInit((CardFactory CardFactory, CardHand CardHand) =>
        {
            Game.RunAfterServiceInit((RelicService RelicService) =>
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

    public GameObject GetGameObject() { return gameObject; }

    public static int GroupCount = 4;
}
