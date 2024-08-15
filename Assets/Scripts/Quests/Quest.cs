using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Actual templated generics quest, including different callbacks
 * Only has a weak ref to the monobehaviour parent, but should still have same lifetime!
 * Only generate through QuestService!
 * T is quest trigger type 
 */
public abstract class Quest<T> : QuestTemplate
{
    public Dictionary<IQuestRegister<T>, ActionList<T>> QuestRegisterMap;
    public bool bIsCancelQuest = false;

    public int CurrentProgress;
    public int MaxProgress;
    public string Description;
    public Type QuestType;
    public Sprite Sprite;

    public abstract int CheckSuccess(T Item);
    public abstract void OnAfterCompletion();

    public abstract int GetStartProgress();
    public abstract Dictionary<IQuestRegister<T>, ActionList<T>> GetRegisterMap();
    public abstract void GrantRewards();
    public override int GetCurrentProgress()
    {
        return CurrentProgress;
    }

    public override bool IsCompleted()
    {
        return CurrentProgress >= MaxProgress;
    }

    public override void Destroy()
    {
        GameObject.DestroyImmediate(Parent.gameObject);
    }

    public Quest() : base(){}

    public override void Init(QuestUIElement Parent)
    {
        this.Parent = Parent;
        CurrentProgress = GetStartProgress();
        MaxProgress = GetMaxProgress();
        Description = GetDescription();
        QuestType = GetQuestType();
        Sprite = GetSprite();
        Register();
        bIsInit = true;
    }

    public override void Register(bool bForceRegister = false)
    {
        if (!ShouldRegister(bForceRegister))
            return;

        QuestRegisterMap = GetRegisterMap();
        if (QuestRegisterMap == null)
            return;

        foreach (var Tuple in QuestRegisterMap)
        {
            Tuple.Key.RegisterQuest(Tuple.Value, this);
        }
        bIsRegistered = true;
    }

    private bool ShouldRegister(bool bForceRegister)
    {
        if (bIsRegistered)
            return false;

        // early exit
        if (bForceRegister)
            return true;

        if (!AreRequirementsFulfilled())
            return false;

        return true;
    }

    public override void OnAccept(bool bIsCanceled = false)
    {
        CompleteQuest(bIsCanceled);
        RemoveQuest(!bIsCanceled);

        Destroy();
    }

    public void OnQuestProgress(T Var)
    {
        if (!bIsInit)
            return;

        CurrentProgress += CheckSuccess(Var);

        Parent.Visualize();

        if (!ShouldAutoComplete() || !IsCompleted())
            return;

        OnAccept(bIsCancelQuest);
    }

    ~Quest()
    {
        RemoveQuestCallback();
    }

    public void CompleteQuest(bool bIsCanceled = false)
    {
        if (!bIsInit)
            return;

        if (!bIsCanceled)
        {
            GrantRewards();
        }
        OnAfterCompletion();
        RemoveQuestCallback();
    }

    public QuestUIElement GetParent()
    {
        return Parent;
    }

    public override void RemoveQuestCallback()
    {
        if (!bIsRegistered || QuestRegisterMap == null || QuestRegisterMap.Count == 0)
            return;

        foreach (var Tuple in QuestRegisterMap)
        {
            Tuple.Key.DeRegisterQuest(Tuple.Value, this);
        }
    }

    public void RemoveQuest(bool bAddFollowup = true)
    {
        if (!Game.TryGetService(out QuestService QuestService))
            return;

        QuestService.RemoveQuest(Parent, bAddFollowup);
    }

    protected void GrantResources(Production Production)
    {

        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddResources(Production);

        Production.Type Type = Production.GetTuples()[0].Key;
        int Amount = Production.GetTuples()[0].Value;
        MessageSystemScreen.CreateMessage(Message.Type.Success, "Completing the quest granted " +Amount+" "+ Type.ToString());
    }

    protected void GrantUpgradePoints(int Count)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddUpgrades(Count);
        MessageSystemScreen.CreateMessage(Message.Type.Success, "Completing the quest granted " + Count+" upgrade points");
    }

    public override void SetCurrentProgress(int Progress)
    {
        CurrentProgress = Progress;
    }

    public override bool ShouldAutoComplete()
    {
        return QuestType == Type.Negative;
    }
}
