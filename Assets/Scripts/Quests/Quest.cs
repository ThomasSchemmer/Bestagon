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

    public abstract int CheckSuccess(T Item);
    public abstract void OnAfterCompletion();

    public abstract int GetStartProgress();
    public abstract int GetMaxProgress();
    public abstract string GetDescription();
    public abstract Type GetQuestType();
    public abstract Sprite GetSprite();
    public abstract IQuestRegister<T> GetRegistrar();
    public abstract List<Action<T>> GetDelegates();
    public abstract void GrantRewards();

    public override void Destroy()
    {
        GameObject.DestroyImmediate(Parent.gameObject);
    }

    public Quest() : base(){}

    public override void Init(QuestUIElement Parent)
    {
        this.Parent = Parent;
        StartProgress = GetStartProgress();
        MaxProgress = GetMaxProgress();
        Description = GetDescription();
        QuestType = GetQuestType();
        Sprite = GetSprite();
        Register();
        bIsInit = true;
    }

    public override void Register()
    {
        if (!ShouldUnlock() || bIsRegistered)
            return;

        QuestRegistrar = GetRegistrar();
        QuestRegistrar.RegisterQuest(GetDelegates(), this);
        bIsRegistered = true;
    }

    public override void OnAccept()
    {
        CompleteQuest();
        RemoveQuest();

        Destroy();
    }

    public void OnQuestProgress(T Var)
    {
        if (!bIsInit)
            return;

        Parent.AddCurrentProgress(CheckSuccess(Var));
        Parent.Visualize();

        if (Parent.GetQuestType() != Type.Negative || !Parent.IsCompleted())
            return;

        OnAccept();
    }

    ~Quest()
    {
        RemoveQuestCallback();
    }

    public void CompleteQuest()
    {
        if (!bIsInit)
            return;

        if (QuestType != Type.Negative)
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
}
