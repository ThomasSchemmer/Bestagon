using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;


/**
 * We cant easily directly store and access a templated object, so use an abstract interface instead
 * Save this instead of the Quest (monobehaviour)
 */
public abstract class QuestTemplate
{
    public enum Type
    {
        Positive,
        Negative,
        Main
    }

    /** Set this to the actual parenting monobehaviour*/
    public QuestUIElement Parent;

    protected bool bIsInit = false;
    protected bool bIsRegistered = false;

    public abstract void RemoveQuestCallback();
    public abstract void OnAccept(bool bIsCanceled = false);
    public abstract void OnCreated();
    public abstract void Destroy();
    public abstract bool AreRequirementsFulfilled();
    /** Can be self-type (repeating the quest) or a follow-up type */
    public abstract bool TryGetNextType(out System.Type Type);
    public abstract void Init(QuestUIElement Parent);
    public abstract void Register(bool bForceAfterLoad = false);
    public abstract int GetCurrentProgress();
    public abstract int GetMaxProgress();
    public abstract Type GetQuestType();
    public abstract string GetDescription();
    public abstract Sprite GetSprite();
    
    public virtual List<System.Type> GetMultiQuestTypes() { return new(); }

    public abstract void SetCurrentProgress(int Progress);

    public abstract bool IsCompleted();

    public QuestTemplate() { }

    public virtual bool ShouldUnlockDirectly() { return false; }

    public virtual bool ShouldAutoComplete() { return false; }
}

/** 
 * We cannot store the actual templated quests as their generic typing interferes with object creation
 * Use this mini-class instead, should just contain the TemplateID for lookup and the current progress
 * Will be used to recreate the actual Quest from the found template
 */
public class QuestTemplateDTO
{
    [SaveableBaseType]
    public Type QuestType;
    [SaveableBaseType]
    public int CurrentProgress;

    public static QuestTemplateDTO CreateFromQuest(QuestTemplate QuestT)
    {
        QuestTemplateDTO DTO = new();
        DTO.CurrentProgress = QuestT.GetCurrentProgress();
        DTO.QuestType = QuestT.GetType();
        return DTO;
    }

}