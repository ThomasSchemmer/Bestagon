using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestService : GameService
{ 
    public Quest<T> CreateQuest<T>(
        Action<Quest<T>> RegisterAction,
        Action<Quest<T>> DeRegisterAction,
        int CurrentProgress,
        int MaxProgress,
        Sprite Sprite,
        Func<T, int> CheckSuccess,
        Action AfterCompletionCallback,
        string Message,
        Quest.Type Type
        )
    {
        GameObject QuestObj = Instantiate(QuestPrefab);

        Quest Quest = QuestObj.AddComponent<Quest>();
        Quest.CurrentProgress = CurrentProgress;
        Quest.MaxProgress = MaxProgress;
        Quest.Sprite = Sprite;
        Quest.Message = Message;
        Quest.QuestType = Type;

        Quest<T> QuestT = new(Quest);
        QuestT.CheckSuccess = CheckSuccess;
        QuestT.AddCompletionCallback(AfterCompletionCallback);
        QuestT.DeRegisterAction = DeRegisterAction;
        RegisterAction.Invoke(QuestT);

        Quest.Add(QuestT);
        DisplayQuests(true);

        return QuestT;
    }

    public Quest<T> AddQuest<T>(
        Action<Quest<T>> RegisterAction,
        Action<Quest<T>> DeRegisterAction,
        int CurrentProgress, 
        int MaxProgress, 
        Sprite Sprite, 
        Func<T, int> CheckSuccess,
        Action AfterCompletionCallback,
        string Message,
        Quest.Type Type
    )
    {
        Quest<T> QuestT = CreateQuest(
            RegisterAction,
            DeRegisterAction,
            CurrentProgress,
            MaxProgress,
            Sprite,
            CheckSuccess,
            AfterCompletionCallback,
            Message,
            Type);
        Quest Quest = QuestT.GetParent();
        Quests.Add(Quest);
        return QuestT;
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


    private void CreateMainQuest()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        MainQuest = CreateQuest<BuildingConfig.Type>(
            Unlockables.RegisterQuest,
            Unlockables.DeregisterQuest,
            0,
            1,
            IconFactory.GetIconForBuildingType(BuildingConfig.Type.Well),
            CheckForWellUnlock,
            CreateNextQuest,
            "Unlock the well to cross the desert",
            Quest.Type.Main
        ).GetParent();
    }

    private void CreateNextQuest()
    {

    }

    private int CheckForWellUnlock(BuildingConfig.Type Type) {
        if (Type != BuildingConfig.Type.Well)
            return 0;

        return 1;
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


    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            CreateMainQuest();
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() { }


    public GameObject QuestPrefab;
    public RectTransform MainQuestContainer, QuestContainer;
    public float RevealSpeed = 10;
    private List<Quest> Quests = new();
    private Quest MainQuest;

    private static float HoverScaleModifier = 1.15f;
    private static Vector3 Origin = new(-100, 50, 0);
    private static Vector3 ShownLocation = new(335 / 2f, 50, 0);
    private static Vector3 QuestOffset = new(0, 15, 0);
    private static Vector3 QuestSize = new(335, 55, 0);
}
