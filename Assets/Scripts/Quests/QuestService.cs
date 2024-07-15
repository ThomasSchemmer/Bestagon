using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

public class QuestService : GameService
{ 
    public Quest AddQuest(Questable Original) {

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

        Quests.Add(Quest);
        DisplayQuests(true);

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

    private void CreateQuestables()
    {
        foreach (Questable Questable in Questables)
        {
            AddQuest(Questable);
        }
    }


    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            CreateQuestables();

            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() { }

    public List<Questable> Questables = new();


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
