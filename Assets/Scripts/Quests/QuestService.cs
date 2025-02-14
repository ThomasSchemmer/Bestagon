using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

public class QuestService : SaveableService
{ 
    public QuestUIElement AddQuest(Type Type) {

        QuestUIElement Quest = CreateQuest(Type);
        ActivateQuest(Quest);    
        return Quest;
    }

    /** Forcing quests allows them to count as unlocked even though the current stats don't allow it
     * Useful for making sure they are actually registered and then saved again correctly
     */
    private void ActivateQuest(QuestUIElement QuestUI, bool bForceAfterLoad = false)
    {
        if (Game.IsIn(Game.GameState.CardSelection))
            return;

        QuestTemplate QuestT = QuestUI.GetQuestObject();
        if (!QuestT.AreRequirementsFulfilled() && !bForceAfterLoad)
        {
            QuestUI.transform.SetParent(InactiveQuestContainer, false);
            QuestsToUnlock.Add(QuestUI);
            return;
        }

        QuestT.Register(bForceAfterLoad);
        QuestT.OnCreated();
        switch (QuestT.GetQuestType())
        {
            case QuestTemplate.Type.Positive: AddPositiveQuest(QuestUI); break;
            case QuestTemplate.Type.Negative: AddNegativeQuest(QuestUI); break;
            case QuestTemplate.Type.Main: AddMainQuest(QuestUI); break;
        }

        QuestUI.Visualize();
        DisplayQuests(true);
    }

    private void AddMainQuest(QuestUIElement Quest)
    {
        if (Quest.GetQuestType() != QuestTemplate.Type.Main)
            return;

        if (MainQuest != null)
        {
            throw new Exception("Cannot add main quest while the old one is still valid!");
        }
        MainQuest = Quest;
    }

    private void AddNegativeQuest(QuestUIElement Quest)
    {
        if (Quest.GetQuestType() != QuestTemplate.Type.Negative)
            return;

        if (NegativeQuest != null)
        {
            throw new Exception("Cannot add negative quest while the old one is still valid!");
        }
        NegativeQuest = Quest;
    }

    private void AddPositiveQuest(QuestUIElement Quest)
    {
        if (Quest.GetQuestType() != QuestTemplate.Type.Positive)
            return;

        PositiveQuests.Insert(0, Quest);
    }

    public QuestUIElement CreateQuest(Type Type)
    {
        GameObject QuestObj = Instantiate(QuestPrefab);

        QuestUIElement QuestElement = QuestObj.AddComponent<QuestUIElement>();

        QuestTemplate Quest = Activator.CreateInstance(Type) as QuestTemplate;
        Quest.Init(QuestElement);

        QuestElement.Init(Quest);
        return QuestElement;
    }

    public void RemoveAllQuests()
    {
        for (int i = PositiveQuests.Count - 1; i >= 0; i--)
        {
            PositiveQuests[i].GetQuestObject().OnAccept(true);
        }
        for (int i = QuestsToUnlock.Count - 1; i >= 0; i--)
        {
            QuestsToUnlock[i].GetQuestObject().OnAccept(true);
        }
        if (MainQuest != null)
        {
            MainQuest.GetQuestObject().OnAccept(true);
        }
        if (NegativeQuest != null)
        {
            NegativeQuest.GetQuestObject().OnAccept(true);
        }
    }

    public void RemoveQuest(QuestUIElement Quest, bool bAddFollowups = true)
    {
        PositiveQuests.Remove(Quest);
        QuestsToUnlock.Remove(Quest);
        HandleFollowUp(Quest, bAddFollowups);
        DisplayQuests();
    }

    private void HandleFollowUp(QuestUIElement Quest, bool bAddFollowups = true)
    {
        // the second of the multi-quest does not have a UI element
        if (Quest == null)
            return;

        QuestTemplate QuestT = Quest.GetQuestObject();
        if (!bAddFollowups || !QuestT.TryGetNextType(out Type Type))
            return;

        QuestUIElement NextQuest = CreateQuest(Type);
        QuestTemplate NextQuestT = NextQuest.GetQuestObject();
        if (NextQuestT.ShouldUnlockDirectly())
        {
            ActivateQuest(NextQuest);
        }
        else
        {
            QuestsToUnlock.Add(NextQuest);
        }
    }

    public void Update()
    {
        DisplayQuests();
    }

    public void DisplayQuests(bool bForceInstant = false)
    {
        DisplayQuest(MainQuest, 0, MainQuestContainer, bForceInstant);

        DisplayQuest(NegativeQuest, 0, NegativeQuestContainer, bForceInstant);

        int Offset = NegativeQuest == null ? 1 : 0;
        for (int i = 0; i < PositiveQuests.Count; i++) {
            DisplayQuest(PositiveQuests[i], i - Offset, QuestContainer, bForceInstant);
        }
    }

    private void DisplayQuest(QuestUIElement Quest, int i, RectTransform TargetTransform, bool bForceInstant = false)
    {
        if (Quest == null)
            return;

        RectTransform QuestTransform = Quest.GetComponent<RectTransform>();
        QuestTransform.SetParent(TargetTransform, false);

        Vector3 Offset = GetQuestOffset(Quest, i, TargetTransform);

        Vector3 Target = Vector3.Lerp(QuestTransform.anchoredPosition, Offset, Time.deltaTime * RevealSpeed);
        Target = bForceInstant ? Offset : Target;
        QuestTransform.anchoredPosition = Target;
        QuestTransform.localScale = Vector3.one * GetHoverModifier(Quest);
    }

    public void CheckForQuestsToUnlock()
    {
        List<QuestUIElement> QuestsToActivate = new();
        foreach (var Quest in QuestsToUnlock)
        {
            if (!Quest.GetQuestObject().AreRequirementsFulfilled())
                continue;

            QuestsToActivate.Add(Quest);
        }

        foreach (QuestUIElement Quest in QuestsToActivate)
        {
            ActivateQuest(Quest);
            QuestsToUnlock.Remove(Quest);
        }
        QuestsToActivate.Clear();
    }

    private Vector2 GetQuestOffset(QuestUIElement Quest, int i, RectTransform TargetTransform)
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

    private float GetHoverModifier(QuestUIElement Quest)
    {
        return Quest.IsHovered() ? HoverScaleModifier : 1;
    }

    private void LoadTemplates()
    {
        LoadQuestTemplates(true);
        // already removed follow-up quests
        foreach (var Type in QuestTypes)
        {
            AddQuest(Type);
        }
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((IconFactory IconFactory, SaveGameManager Manager) =>
        {
            if (!Game.IsIn(Game.GameState.CardSelection) && !Manager.HasDataFor(SaveableService.SaveGameType.Quests))
            {
                LoadTemplates();
            }

            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() { }

    protected override void ResetInternal()
    {
        
        RemoveAllQuests();
        if (MainQuest != null)
        {
            MainQuest.InvokeDestroy();
            MainQuest = null;
        }
        if (NegativeQuest != null)
        {
            NegativeQuest.InvokeDestroy();
            NegativeQuest = null;
        }
        foreach (QuestUIElement Quest in PositiveQuests)
        {
            Quest.InvokeDestroy();
        }
        foreach (QuestUIElement Quest in QuestsToUnlock)
        {
            Quest.InvokeDestroy();
        }
        PositiveQuests.Clear();
        QuestsToUnlock.Clear();
    }

    private int GetQuestCount()
    {
        // count main quest as well
        return PositiveQuests.Count + (MainQuest != null ? 1 : 0) + (NegativeQuest != null ? 1 : 0);
    }

    private int GetInactiveQuestCount()
    {
        return QuestsToUnlock.Count;
    }

    private void HandleTutorialInit()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;

        if (!TutorialSystem.IsInTutorial())
            return;

        TutorialSystem.SetInTutorial(false);
        LoadTemplates();
    }

    private void LoadQuestFromSavegame(QuestTemplateDTO DTO, out QuestUIElement Quest)
    {
        Quest = default;
        int CurrentProgress = DTO.CurrentProgress;
        if (!TryGetQuestType(DTO, out Type FoundType))
            return;

        Quest = CreateQuest(FoundType);
        Quest.GetQuestObject().SetCurrentProgress(CurrentProgress);
    }

    private bool TryGetQuestType(QuestTemplateDTO DTO, out Type FoundType)
    {
        FoundType = default;
        foreach (var Type in QuestTypes)
        {
            if (Type != DTO.QuestType)
                continue;

            FoundType = Type;
            return true;
        }

        return false;
    }

    private void LoadQuestTemplates(bool bRemoveFollowUps)
    {
        QuestTypes = new();

        Type BaseType = typeof(QuestTemplate);
        Type[] Types = Assembly.GetAssembly(typeof(QuestTemplate)).GetTypes();
        Types = Types.Where(type => type.IsSubclassOf(BaseType) && !type.IsAbstract).ToArray();

        foreach (var Type in Types)
        {
            QuestTemplate Template = Activator.CreateInstance(Type) as QuestTemplate;
            QuestTypes.Add(Type);
        }
        if (!bRemoveFollowUps)
            return;

        List<Type> TypesToRemove = new();
        foreach (var Type in QuestTypes)
        {
            QuestTemplate Template = Activator.CreateInstance(Type) as QuestTemplate;
            List<Type> ToRemove = GetTypesToRemove(Template);

            TypesToRemove.AddRange(ToRemove);
        }
        foreach (Type Type in TypesToRemove)
        {
            QuestTypes.Remove(Type);
        }
    }

    /** Filters out quests that are either follow-ups or part of multiquest */
    private List<Type> GetTypesToRemove(QuestTemplate Template)
    {
        List<Type> TypesToRemove = new();

        TypesToRemove.AddRange(Template.GetMultiQuestTypes());

        if (!Template.TryGetNextType(out Type NextType))
            return TypesToRemove;

        // repeated quest should not be removed
        if (NextType == Template.GetType())
            return TypesToRemove;

        TypesToRemove.Add(NextType);
        return TypesToRemove;
    }

    public void Show(bool bShow)
    {
        transform.GetChild(0).gameObject.SetActive(bShow);
    }

    public override void OnAfterLoaded()
    {
        LoadQuestTemplates(false);
        PositiveQuests = new();

        if (MainQuestDTO != null)
        {
            LoadQuestFromSavegame(MainQuestDTO, out QuestUIElement Quest);
            ActivateQuest(Quest, false);
        }
        if (NegativeQuestDTO != null)
        {
            LoadQuestFromSavegame(NegativeQuestDTO, out QuestUIElement Quest);
            ActivateQuest(Quest, false);
        }
        foreach (var QuestDTO in PositiveQuestsDTO)
        {
            LoadQuestFromSavegame(QuestDTO, out QuestUIElement Quest);
            ActivateQuest(Quest, false);
        }
        foreach (var QuestDTO in QuestsToUnlockDTO)
        {
            LoadQuestFromSavegame(QuestDTO, out QuestUIElement Quest);
            ActivateQuest(Quest, false);
        }

        _OnInit?.Invoke(this);
    }

    public override void OnBeforeSaved(bool bShouldReset)
    {
        HandleTutorialInit();

        // if the user saves multiple times, this has to be reset
        // in card selection the user can't save and the quests are also not actually loaded
        // => keeep DTOs
        if (!Game.IsIn(Game.GameState.CardSelection))
        {
            MainQuestDTO = null;
            NegativeQuestDTO = null;
            PositiveQuestsDTO.Clear();
            QuestsToUnlockDTO.Clear();
        }

        if (MainQuest != null)
        {
            MainQuestDTO = QuestTemplateDTO.CreateFromQuest(MainQuest.GetQuestObject());
        }
        if (NegativeQuest != null)
        {
            NegativeQuestDTO = QuestTemplateDTO.CreateFromQuest(NegativeQuest.GetQuestObject());
        }
        foreach (var Quest in PositiveQuests)
        {
            PositiveQuestsDTO.Add(QuestTemplateDTO.CreateFromQuest(Quest.GetQuestObject()));
        }
        foreach (var Quest in QuestsToUnlock)
        {
            QuestsToUnlockDTO.Add(QuestTemplateDTO.CreateFromQuest(Quest.GetQuestObject()));
        }
    }

    public GameObject GetGameObject() { return gameObject; }

    public QuestUIElement MainQuest;
    public QuestUIElement NegativeQuest;
    public List<QuestUIElement> PositiveQuests = new();
    public List<QuestUIElement> QuestsToUnlock = new();

    [SaveableClass]
    public QuestTemplateDTO MainQuestDTO;
    [SaveableClass]
    public QuestTemplateDTO NegativeQuestDTO;
    [SaveableList]
    public List<QuestTemplateDTO> PositiveQuestsDTO = new();
    [SaveableList]
    public List<QuestTemplateDTO> QuestsToUnlockDTO = new();

    protected HashSet<Type> QuestTypes = new();

    public GameObject QuestPrefab;
    public RectTransform MainQuestContainer, NegativeQuestContainer, QuestContainer, InactiveQuestContainer;
    public float RevealSpeed = 10;

    private static float HoverScaleModifier = 1.15f;
    private static Vector3 Origin = new(-100, 50, 0);
    private static Vector3 ShownLocation = new(335 / 2f, 50, 0);
    private static Vector3 QuestOffset = new(0, 15, 0);
    private static Vector3 QuestSize = new(335, 55, 0);
}
