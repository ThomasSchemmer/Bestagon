using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;



/** 
 * Actual templated quest, including different callbacks
 * Only has a weak ref to the monobehaviour parent, but should still have same lifetime!
 * Do not generate directly (except from savegame) - should be created from questable!
 */
public class Quest<T> : QuestTemplate
{
    public override void Destroy()
    {
        _OnQuestCompleted = null;
        GameObject.DestroyImmediate(Parent.gameObject);
    }

    public override void OnAccept()
    {
        CompleteQuest();
        RemoveQuest();

        Destroy();
    }

    public void OnQuestProgress(T Var)
    {
        Parent.CurrentProgress += CheckSuccess(Var);
        Parent.Visualize();

        if (Parent.QuestType != Quest.Type.Negative || !Parent.IsCompleted())
            return;

        OnAccept();
    }

    public Quest(Quest Parent)
    {
        this.Parent = Parent;
    }

    ~Quest()
    {
        RemoveQuestCallback();
    }

    public void CompleteQuest()
    {
        RemoveQuestCallback();
        _OnQuestCompleted?.Invoke();
        _OnQuestCompleted = null;
    }

    public void AddCompletionCallback(Action Callback)
    {
        _OnQuestCompleted += () =>
        {
            Callback();
        };
    }

    public Quest GetParent()
    {
        return Parent;
    }

    public override void RemoveQuestCallback()
    {
        DeRegisterAction.Invoke(this);
    }

    public void RemoveQuest()
    {
        if (!Game.TryGetService(out QuestService QuestService))
            return;

        QuestService.RemoveQuest(Parent);

        if (FollowUpQuest == null)
            return;

        QuestService.AddQuest(FollowUpQuest);
    }

    public Func<T, int> CheckSuccess;
    public Action<Quest<T>> DeRegisterAction;
    public Questable FollowUpQuest;

    public delegate void OnQuestCompleted();
    public event OnQuestCompleted _OnQuestCompleted;
}

/**
 * We cant easily directly store and access a templated object, so use an abstract interface instead
 * Save this instead of the Quest (monobehaviour)
 */
public abstract class QuestTemplate
{
    /** Set this to the actual parenting monobehaviour*/
    public Quest Parent;
    /** used for save only, references the Questable to load */
    public int QuestableID;
    /** Function ptr which returns true if it should be unlocked */
    public Func<bool> Unlock;

    public abstract void RemoveQuestCallback();
    public abstract void OnAccept();
    public abstract void Destroy();

}

/** 
 * We cannot store the actual templated quests as their generic typing interferes with object creation
 * Use this mini-class instead, should just contain the QuestableID for lookup and the current progress
 * Will be used to recreate the actual Quest from the found Questable
 */
public class QuestTemplateDTO : ISaveableData
{
    public int QuestableID;
    public float CurrentProgress;

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // id and current progress
        return sizeof(int) + sizeof(double);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetStaticSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, QuestableID);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, CurrentProgress);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out QuestableID);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dCurrentProgress);
        CurrentProgress = (float)dCurrentProgress;
    }

    public static QuestTemplateDTO CreateFromQuest(QuestTemplate Quest)
    {
        QuestTemplateDTO DTO = new();
        DTO.CurrentProgress = Quest.Parent.CurrentProgress;
        DTO.QuestableID = Quest.QuestableID;
        return DTO;
    }
}