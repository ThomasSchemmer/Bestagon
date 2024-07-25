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
    public IQuestRegister<T> QuestRegistrar;
    public bool bIsCancelQuest = false;

    public int CurrentProgress;
    public int MaxProgress;
    public string Description;
    public Type QuestType;
    public Sprite Sprite;

    public abstract int CheckSuccess(T Item);
    public abstract void OnAfterCompletion();

    public abstract int GetStartProgress();
    public abstract IQuestRegister<T> GetRegistrar();
    public abstract ActionList<T> GetDelegates();
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

        QuestRegistrar = GetRegistrar();
        if (QuestRegistrar == null)
            return;

        QuestRegistrar.RegisterQuest(GetDelegates(), this);
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
        RemoveQuest();

        Destroy();
    }

    public void OnQuestProgress(T Var)
    {
        if (!bIsInit)
            return;

        CurrentProgress += CheckSuccess(Var);

        Parent.Visualize();

        // negative quests auto-complete
        if (QuestType != Type.Negative || !IsCompleted())
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
        if (!bIsRegistered)
            return;

        QuestRegistrar.DeRegisterQuest(GetDelegates(), this);
    }

    public void RemoveQuest()
    {
        if (!Game.TryGetService(out QuestService QuestService))
            return;

        QuestService.RemoveQuest(Parent);
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

        Stockpile.UpgradePoints += Count;
        MessageSystemScreen.CreateMessage(Message.Type.Success, "Completing the quest granted " + Count+" upgrade points");
    }

    public override void SetCurrentProgress(int Progress)
    {
        CurrentProgress = Progress;
    }
}
