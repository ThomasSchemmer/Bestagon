using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Provides functionality to create a self-cancelable quest, ie a quest with two different goals */
public abstract class MultiQuest<T, C> : QuestTemplate
{
    public Quest<T> Quest;
    public Quest<C> CancelQuest;

    public abstract System.Type GetMultiQuestType();
    public abstract System.Type GetCancelQuestType();

    public override List<System.Type> GetMultiQuestTypes() {
        return new() { GetMultiQuestType(), GetCancelQuestType() };
    }

    public override void Register(bool bForceAfterLoad = false)
    {
        Quest.Register(bForceAfterLoad);
        CancelQuest.Register(bForceAfterLoad);
    }

    public override void Init(QuestUIElement Parent)
    {
        Quest = Activator.CreateInstance(GetMultiQuestType()) as Quest<T>;
        CancelQuest = Activator.CreateInstance(GetCancelQuestType()) as Quest<C>;
        Quest.Init(Parent);
        CancelQuest.Init(Parent);
        CancelQuest.bIsCancelQuest = true;
    }

    public override void Destroy()
    {
        Quest.Destroy();
        CancelQuest.Destroy();
    }

    public override void OnAccept(bool bIsCanceled = false)
    {
        Quest.OnAccept(bIsCanceled);
        CancelQuest.OnAccept(bIsCanceled);
    }

    public override void RemoveQuestCallback()
    {
        Quest.RemoveQuestCallback();
        CancelQuest.RemoveQuestCallback();
    }

    public override int GetCurrentProgress()
    {
        return Quest.GetCurrentProgress();
    }
    public override int GetMaxProgress()
    {
        return Quest.GetMaxProgress();
    }
    public override Type GetQuestType()
    {
        return Quest.GetQuestType();
    }
    public override string GetDescription()
    {
        return Quest.GetDescription();
    }
    public override Sprite GetSprite()
    {
        return Quest.GetSprite();
    }

    public override bool IsCompleted()
    {
        return Quest.IsCompleted();
    }   

    public override void SetCurrentProgress(int Progress)
    {
        Quest.CurrentProgress = Progress;
    }

}
