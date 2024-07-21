using System;
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

    public int StartProgress;
    public int MaxProgress;
    public string Description;
    public Type QuestType;
    public Sprite Sprite;

    protected bool bIsInit = false;
    protected bool bIsRegistered = false;

    public abstract void RemoveQuestCallback();
    public abstract void OnAccept();
    public abstract void Destroy();
    public abstract bool ShouldUnlock();
    /** Can be self-type (repeating the quest) or a follow-up type */
    public abstract bool TryGetNextType(out System.Type Type);
    public abstract void Init(QuestUIElement Parent);
    public abstract void Register();

    public QuestTemplate() { }
}

/** 
 * We cannot store the actual templated quests as their generic typing interferes with object creation
 * Use this mini-class instead, should just contain the TemplateID for lookup and the current progress
 * Will be used to recreate the actual Quest from the found template
 */
public class QuestTemplateDTO : ISaveableData
{
    public string TypeName;
    public float CurrentProgress;

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // name, name length and current progress
        return sizeof(byte) * MAX_NAME_LENGTH + sizeof(double);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddString(Bytes, Pos, TypeName);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, CurrentProgress);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetString(Bytes, Pos, MAX_NAME_LENGTH, out TypeName);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dCurrentProgress);
        CurrentProgress = (float)dCurrentProgress;
        TypeName = TypeName.Replace(NAME_PADDING_CHAR+ "", "");
    }

    public static QuestTemplateDTO CreateFromQuest(QuestTemplate QuestT)
    {
        QuestTemplateDTO DTO = new();
        DTO.CurrentProgress = QuestT.Parent.GetCurrentProgress();
        DTO.TypeName = GetReplacedName(QuestT.GetType());
        return DTO;
    }

    private static string GetReplacedName(Type Type)
    {
        string TypeName = GetCutName(Type);
        int Offset = Mathf.Max(TypeName.Length, MAX_NAME_LENGTH);
        return TypeName.PadRight(Offset, NAME_PADDING_CHAR);
    }

    public static string GetCutName(Type Type)
    {
        int Offset = Mathf.Min(Type.Name.Length, MAX_NAME_LENGTH);
        return Type.Name[..Offset];
    }

    public static int MAX_NAME_LENGTH = 20;
    public static char NAME_PADDING_CHAR = '=';
}