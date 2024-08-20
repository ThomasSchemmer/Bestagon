using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Provides functionality to create a quest with two different goals 
 * The last quest can be self-cancelling, i.e. cancelling the MultiQuest on completion
 */
public abstract class MultiQuest<T1, T2> : QuestTemplate
{
    public Quest<T1> Quest1;
    public Quest<T2> Quest2;

    public abstract System.Type GetQuest1Type();
    public abstract System.Type GetQuest2Type();
    public abstract bool IsQuest2Cancel();

    public override List<System.Type> GetMultiQuestTypes() {
        return new() { GetQuest1Type(), GetQuest2Type() };
    }

    public override void Register(bool bForceAfterLoad = false)
    {
        Quest1.Register(bForceAfterLoad);
        Quest2.Register(bForceAfterLoad);
    }

    public override void Init(QuestUIElement Parent)
    {
        Quest1 = Activator.CreateInstance(GetQuest1Type()) as Quest<T1>;
        Quest2 = Activator.CreateInstance(GetQuest2Type()) as Quest<T2>;
        Quest1.Init(Parent);
        Quest2.Init(Parent);
        Quest2.bIsCancelQuest = IsQuest2Cancel();
    }

    public override void Destroy()
    {
        Quest1.Destroy();
        Quest2.Destroy();
    }

    public override void OnAccept(bool bIsCanceled = false)
    {
        Quest1.OnAccept(bIsCanceled);
        Quest2.OnAccept(bIsCanceled);
    }

    public override void RemoveQuestCallback()
    {
        Quest1.RemoveQuestCallback();
        Quest2.RemoveQuestCallback();
    }

    public override int GetCurrentProgress()
    {
        return Quest1.GetCurrentProgress();
    }
    public override int GetMaxProgress()
    {
        return Quest1.GetMaxProgress();
    }
    public override Type GetQuestType()
    {
        return Quest1.GetQuestType();
    }
    public override string GetDescription()
    {
        return Quest1.GetDescription();
    }
    public override Sprite GetSprite()
    {
        return Quest1.GetSprite();
    }

    public override bool IsCompleted()
    {
        return Quest1.IsCompleted() && (IsQuest2Cancel() || Quest2.IsCompleted());
    }   

    public override void SetCurrentProgress(int Progress)
    {
        Quest1.CurrentProgress = Progress;
    }

}
