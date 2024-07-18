using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class QuestService : GameService, ISaveableService
{ 
    public Quest AddQuest(Questable Original) {

        Quest Quest = CreateQuest(Original);
        AddQuest(Quest);    
        return Quest;
    }

    private Quest AddQuest(Quest Quest)
    {
        AddMainQuest(Quest);
        AddRegularQuest(Quest);
        DisplayQuests(true);
        return Quest;
    }

    private void AddMainQuest(Quest Quest)
    {
        if (Quest.QuestType != Quest.Type.Main)
            return;

        if (MainQuest != null)
        {
            throw new Exception("Cannot add main quest while the old one is still valid!");
        }
        MainQuest = Quest;
    }

    private void AddRegularQuest(Quest Quest)
    {
        if (Quest.QuestType == Quest.Type.Main)
            return;

        QuestTemplate Q = Quest.GetQuestObject();
        bool bShouldBeUnlocked = Q.Unlock == null || Q.Unlock.Invoke();
        List<Quest> Target = bShouldBeUnlocked ? Quests : QuestsToUnlock;

        Target.Add(Quest);
    }

    public Quest CreateQuest(Questable Original)
    {
        GameObject QuestObj = Instantiate(QuestPrefab);

        Questable Copy = Instantiate(Original);
        Copy.Init();

        Quest Quest = QuestObj.AddComponent<Quest>();
        Quest.CurrentProgress = Copy.StartProgress;
        Quest.MaxProgress = Copy.MaxProgress;
        Quest.Message = Copy.Description;
        Quest.Sprite = Copy.Sprite;
        Quest.QuestType = Copy.QuestType;
        Copy.AddGenerics(Quest);
        return Quest;
    }

    public void RemoveQuest(Quest Quest)
    {
        Quests.Remove(Quest);
        DisplayQuests();
    }

    public void Update()
    {
        DisplayQuests();
    }

    public void DisplayQuests(bool bForceInstant = false)
    {
        DisplayQuest(MainQuest, 0, true, bForceInstant);

        for (int i = 0; i < Quests.Count; i++) {
            DisplayQuest(Quests[i], i, false, bForceInstant);
        }
    }

    private void DisplayQuest(Quest Quest, int i, bool bIsMainQuest, bool bForceInstant = false)
    {
        if (Quest == null)
            return;

        RectTransform QuestTransform = Quest.GetComponent<RectTransform>();
        RectTransform TargetTransform = bIsMainQuest ? MainQuestContainer : QuestContainer;
        QuestTransform.SetParent(TargetTransform, false);

        Vector3 Offset = GetQuestOffset(Quest, i, TargetTransform);

        Vector3 Target = Vector3.Lerp(QuestTransform.anchoredPosition, Offset, Time.deltaTime * RevealSpeed);
        Target = bForceInstant ? Offset : Target;
        QuestTransform.anchoredPosition = Target;
        QuestTransform.localScale = Vector3.one * GetHoverModifier(Quest);
    }

    public void CheckForQuestsToUnlock()
    {
        List<Quest> QuestsToActivate = new();
        foreach (var Quest in QuestsToUnlock)
        {
            if (!Quest.GetQuestObject().Unlock.Invoke())
                continue;

            QuestsToActivate.Add(Quest);
        }

        foreach (Quest Quest in QuestsToActivate)
        {
            Quests.Add(Quest);
            QuestsToUnlock.Remove(Quest);
        }
        QuestsToActivate.Clear();
    }

    private Vector2 GetQuestOffset(Quest Quest, int i, RectTransform TargetTransform)
    {
        float Scale = GetHoverModifier(Quest);
        float HalfScale = 1 + (Scale - 1) / 2f;
        float XOffsetHover = QuestSize.x * HalfScale - Origin.x;
        float XOffsetNormal = QuestSize.x / 2f;
        float XOffset = Quest.IsHovered() ? XOffsetHover : XOffsetNormal;
        // y position should not change on hover
        float YOffset = GetQuestYOffset(i, TargetTransform);
        return new(XOffset, YOffset);
    }

    private float GetQuestYOffset(int i, RectTransform TargetTransform)
    {
        StockpileGroupScreen Selected = StockpileGroupScreen.GetSelectedInstance();
        float Offset = Selected == null ? 0 : Selected.GetContainerHeight();
        Vector3 TargetSize = TargetTransform.sizeDelta;
        float ElementOffset = (TargetSize.y - QuestSize.y) / 2 - i * (QuestSize.y + QuestOffset.y);
        return -Offset + ElementOffset;
    }

    private float GetHoverModifier(Quest Quest)
    {
        return Quest.IsHovered() ? HoverScaleModifier : 1;
    }

    private void LoadQuestables()
    {
        CreateQuestableLookup(true);
        foreach (var Tuple in QuestableLookup)
        {
            // already removed follow-up quests
            if (Tuple.Value == null)
                continue;

            AddQuest(Tuple.Value);
        }
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            if (!Game.IsIn(Game.GameState.CardSelection))
            {
                LoadQuestables();
            }

            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() { }

    public void Reset()
    {
        if (MainQuest != null)
        {
            MainQuest.InvokeDestroy();
            MainQuest = null;
        }
        foreach (Quest Quest in Quests)
        {
            Quest.InvokeDestroy();
        }
        Quests.Clear();
    }

    public int GetSize()
    {
        // add count and overall size
        return QuestTemplateDTO.GetStaticSize() * GetQuestCount() + sizeof(int) * 2; 
    }

    private int GetQuestCount()
    {
        // count main quest as well
        return Quests.Count + 1;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        // save the size to make reading it easier
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetQuestCount());

        foreach (Quest Quest in Quests)
        {
            QuestTemplateDTO DTO = QuestTemplateDTO.CreateFromQuest(Quest.GetQuestObject());
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, DTO);
        }
        QuestTemplateDTO MainDTO = QuestTemplateDTO.CreateFromQuest(MainQuest.GetQuestObject());
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, MainDTO);

        return Bytes.ToArray();
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public void SetData(NativeArray<byte> Bytes)
    {
        CreateQuestableLookup();

        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int QuestLength);

        Quests = new();
        for (int i = 0; i < QuestLength - 1; i++)
        {
            Pos = LoadQuestFromSavegame(Bytes, Pos, out Quest Quest);
            AddQuest(Quest);
        }
        Pos = LoadQuestFromSavegame(Bytes, Pos, out Quest MainQuest);
        AddQuest(MainQuest);
    }
    private int LoadQuestFromSavegame(NativeArray<byte> Bytes, int Pos, out Quest Quest)
    {
        Quest = default;
        QuestTemplateDTO DTO = new();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, DTO);

        float CurrentProgress = DTO.CurrentProgress;
        if (!QuestableLookup.ContainsKey(DTO.QuestableID))
            return Pos;

        Questable Questable = QuestableLookup[DTO.QuestableID];
        Quest = CreateQuest(Questable);
        Quest.CurrentProgress = CurrentProgress;
        return Pos;
    }

    private void CreateQuestableLookup(bool bShouldRemoveFollowUps = false)
    {
        QuestableLookup = new();
        var QuestableObjects = Resources.LoadAll("Quests", typeof(Questable));
        foreach (var QuestableObject in QuestableObjects)
        {
            Questable Questable = QuestableObject as Questable;
            if (QuestableLookup.ContainsKey(Questable.ID))
            {
                throw new Exception("Questables must have unique IDs! See conflict for ID "+Questable.ID+" between " +
                    Questable.name + " and " + QuestableLookup[Questable.ID].name);
            }
            QuestableLookup.Add(Questable.ID, Questable);
        }
        if (!bShouldRemoveFollowUps)
            return;

        List<int> IDsToRemove = new();
        foreach (var Tuple in QuestableLookup)
        {
            if (Tuple.Value.FollowUpQuest == null)
                continue;

            int ID = Tuple.Value.FollowUpQuest.ID;
            IDsToRemove.Add(ID);
        }
        foreach (int ID in IDsToRemove)
        {
            QuestableLookup.Remove(ID);
        }
    }
    
    public Quest MainQuest;
    public List<Quest> Quests = new();
    public List<Quest> QuestsToUnlock = new();

    protected Dictionary<int, Questable> QuestableLookup = new();

    public GameObject QuestPrefab;
    public RectTransform MainQuestContainer, QuestContainer;
    public float RevealSpeed = 10;

    private static float HoverScaleModifier = 1.15f;
    private static Vector3 Origin = new(-100, 50, 0);
    private static Vector3 ShownLocation = new(335 / 2f, 50, 0);
    private static Vector3 QuestOffset = new(0, 15, 0);
    private static Vector3 QuestSize = new(335, 55, 0);
}
