using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CheatService : ScreenUI
{
    public TMPro.TMP_InputField CheatText;

    protected bool bIsEnabled = false;

    void Start()
    {
        Initialize();
        Hide();
    }

    public void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F3))
            return;

#if UNITY_EDITOR
        bIsEnabled = !bIsEnabled;
#endif

        if (bIsEnabled)
        {
            Game.Instance.OnOpenMenu();

            EventSystem.current.SetSelectedGameObject(CheatText.gameObject, null);
            Show();
        }
        if (!bIsEnabled)
        {
            Game.Instance.OnCloseMenu();
            Hide();
        }
    }

    public void OnEndEdit()
    {
        ActivateCheat();
        CheatText.text = string.Empty;
    }

    private void ActivateCheat()
    {
        string Cheat = CheatText.text;
        string[] Cheats = Cheat.Split(TOKEN_DIVIDER);
        if (Cheats.Length == 0)
            return;

        if (Cheats[0].Equals(UNLOCK_CODE))
        {
            Unlock(Cheats);
        }
        if (Cheats[0].Equals(CARD_CODE))
        {
            GiveCard(Cheats);
        }
        if (Cheats[0].Equals(RESOURCE_CODE))
        {
            GiveResource(Cheats);
        }
        if (Cheats[0].Equals(QUEST_CODE))
        {
            GiveQuest(Cheats);
        }
        if (Cheats[0].Equals(DESTROY_CARDS_CODE))
        {
            DestroyAllCards(Cheats);
        }
        if (Cheats[0].Equals(DESTROY_UNITS_CODE))
        {
            DestroyAllUnits(Cheats);
        }
        if (Cheats[0].Equals(DESTROY_QUESTS_CODE))
        {
            DestroyAllQuests(Cheats);
        }
        if (Cheats[0].Equals(DESTROY_RESOURCES_CODE))
        {
            DestroyAllResources(Cheats);
        }
        if (Cheats[0].Equals(VANISH_CODE))
        {
            DestroyAllCards(Cheats);
            DestroyAllUnits(Cheats);
            DestroyAllQuests(Cheats);
            DestroyAllResources(Cheats);
        }
    }

    private void GiveQuest(string[] Cheats)
    {
        if (!Game.TryGetService(out QuestService QuestService))
            return;

        string Name = GetTargetName(Cheats);
        if (!TryGetTargetType(Name, typeof(QuestTemplate), out Type QuestType))
            return;

        QuestService.AddQuest(QuestType);
    }

    private void DestroyAllResources(string[] Cheats)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Production InvertedResources = new();
        var Tuples = Stockpile.Resources.GetTuples();
        for (int i = 0; i < Tuples.Count; i++)
        {
            InvertedResources += new Production(Tuples[i].Key, -Tuples[i].Value);
        }

        Stockpile.AddResources(InvertedResources);
    }

    private void DestroyAllQuests(string[] Cheats)
    {
        if (!Game.TryGetService(out QuestService Quests))
            return;

        Quests.RemoveAllQuests();
    }

    private void DestroyAllCards(string[] Cheats)
    {
        if (!Game.TryGetServices(out CardHand CardHand, out CardDeck CardDeck))
            return;

        CardHand.DeleteAllCardsConditionally((Card Card) =>
        {
            return true;
        });
        CardDeck.DeleteAllCardsConditionally((Card Card) =>
        {
            return true;
        });
    }

    private void DestroyAllUnits(string[] Cheats)
    {
        if (!Game.TryGetServices(out Units Units, out Workers Workers))
            return;

        Units.KillAllUnits();
        Workers.KillAllUnits();
    }

    private void GiveResource(string[] Cheats) {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        string TargetName = GetTargetName(Cheats);
        int TargetValue = GetTargetValue(Cheats);
        int TargetIndex = GetTargetIndex(TargetName, typeof(Production.Type));
        if (TargetIndex < 0)
            return;

        Production.Type TargetType = (Production.Type)(TargetIndex);
        Stockpile.AddResources(new Production(TargetType, TargetValue));
    }

    private void GiveCard(string[] Cheats) {
        if (!Game.TryGetServices(out CardFactory CardFactory, out CardHand CardHand))
            return;

        if (!Game.TryGetService(out Unlockables Unlockables))
            return;

        string TargetName = GetTargetName(Cheats);
        int TargetIndex = GetTargetIndex(TargetName, typeof(BuildingConfig.Type));
        if (TargetIndex >= 0)
        {
            // BuildingType is a flag system with default at index 0
            TargetIndex = HexagonConfig.IntToMask(TargetIndex - 1);
            BuildingConfig.Type TargetType = (BuildingConfig.Type)(TargetIndex);
            Unlockables.UnlockSpecificBuildingType(TargetType);
            CardFactory.CreateCard(TargetType, 0, CardHand.transform, CardHand.AddCard);
            return;
        }
        TargetIndex = GetTargetIndex(TargetName, typeof(UnitData.UnitType));
        if (TargetIndex >= 0)
        {
            UnitData.UnitType TargetType = (UnitData.UnitType)(TargetIndex);
            CardFactory.CreateCard(TargetType, 0, CardHand.transform, CardHand.AddCard);
            return;
        }
        TargetIndex = GetTargetIndex(TargetName, typeof(EventData.EventType));
        if (TargetIndex >= 0)
        {
            EventData.EventType TargetType = (EventData.EventType)(TargetIndex);
            CardFactory.CreateCard(TargetType, 0, CardHand.transform, CardHand.AddCard);
            return;
        }
    }

    private string GetTargetName(string[] Cheats)
    {
        string TargetName = Cheats[1].ToLower();
        TargetName = TargetName.Substring(0, TargetName.Length);
        return TargetName;
    }

    private int GetTargetValue(string[] Cheats)
    {
        string TargetValue = Cheats[2];
        TargetValue = TargetValue.Substring(0, TargetValue.Length);
        return int.Parse(TargetValue);
    }

    private int GetTargetIndex(string TargetName, Type T)
    {
        int TargetIndex = -1;
        string[] TypeNames = Enum.GetNames(T);
        for (int i = 0; i < TypeNames.Length; i++)
        {
            if (!TypeNames[i].ToLower().Equals(TargetName))
                continue;

            TargetIndex = i;
            break;
        }
        return TargetIndex;
    }

    private bool TryGetTargetType(string TargetName, Type RootType, out Type TargetType)
    {
        Type[] Types = Assembly.GetAssembly(RootType).GetTypes();
        Types = Types.Where(type => type.IsSubclassOf(RootType) && !type.IsAbstract).ToArray();
        foreach (Type Type in Types)
        {
            if (!Type.FullName.ToLower().Equals(TargetName.ToLower()))
                continue;

            TargetType = Type;
            return true;
        }

        TargetType = default;
        return false;
    }

    private void Unlock(string[] Cheats)
    {
        string TargetName = GetTargetName(Cheats);
        int TargetIndex = GetTargetIndex(TargetName, typeof(BuildingConfig.Type));
        if (TargetIndex == -1)
            return;

        if (!Game.TryGetService(out Unlockables Unlockables))
            return;

        BuildingConfig.Type TargetType = (BuildingConfig.Type)HexagonConfig.IntToMask(TargetIndex - 1);
        Unlockables.UnlockSpecificBuildingType(TargetType);
        Debug.Log("Unlocked " + TargetType);
    }

    private static string TOKEN_DIVIDER = " ";
    private static string UNLOCK_CODE = "unlock";
    private static string CARD_CODE = "givecard";
    private static string RESOURCE_CODE = "giveresource";
    private static string QUEST_CODE = "givequest";
    private static string DESTROY_CARDS_CODE = "destroyallcards";
    private static string DESTROY_UNITS_CODE = "destroyallunits";
    private static string DESTROY_QUESTS_CODE = "destroyallquests";
    private static string DESTROY_RESOURCES_CODE = "destroyallresources";
    private static string VANISH_CODE = "vanish";
}
