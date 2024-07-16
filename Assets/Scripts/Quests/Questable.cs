using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/** Stores quests in scriptable objects for better management
 * Will be converted on loading into an actual quest
 * Should not be saved in savegame, only asset/lookup!
 */
[Serializable]
public abstract class Questable : ScriptableObject
{
    public int StartProgress;
    public int MaxProgress;
    public string Description;
    public Quest.Type QuestType;
    public Sprite Sprite;
    public int ID;

    /** Will be created once this quest is completed*/
    public Questable FollowUpQuest;

    /** Script that allows the quest to register to events, must be a service!
     * Automatically uses the functions declared in @IQuestCompleter
     */
    public MonoScript TriggerRegisterScript;
    /** Script that will be called on certain quest events, eg check for fulfillment */
    public MonoScript CallbackScript;

    public Type TriggerRegisterType;
    public Type CallbackType;
    public string CheckSuccessName;
    public string OnCompletionName;

    public abstract bool TrySetTriggerRegister();
    public abstract bool TrySetCallbacks();
    public abstract void Init();
    public abstract void AddGenerics(Quest Quest);
    protected abstract bool IsValidCheckSuccess(MethodInfo Info);


    public string[] GetQuestableMethods(bool bIsForCheckSuccess)
    {
        string[] List = new string[0];
        Type TargetType = CallbackType;
        if (TargetType == null)
            return List;

        var Infos = TargetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic)
                    .Where(_ => IsValidInfo(_, bIsForCheckSuccess));
        List = new string[Infos.Count()];
        for (int i = 0; i < Infos.Count(); i++)
        {
            List[i] = Infos.ElementAt(i).Name;
        }
        return List;
    }

    public MethodInfo GetInfoForCallback(bool bIsForCheckSuccess)
    {
        Type TargetType = CallbackType;
        if (TargetType == null)
            return null;

        string Name = bIsForCheckSuccess ? CheckSuccessName : OnCompletionName;
        if (Name.Equals(string.Empty))
            return null;

        var Infos = TargetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic)
                    .Where(_ => IsValidInfo(_, bIsForCheckSuccess));

        foreach (var Info in Infos)
        {
            if (!Info.Name.Equals(Name))
                continue;

            return Info;
        }

        return null;
    }

    public MethodInfo GetStaticInfoForTrigger(bool bIsForRegister)
    {
        Type TargetType = TriggerRegisterType;
        if (TargetType == null)
            return null;

        string Name = bIsForRegister ? REGISTER_NAME : DEREGISTER_NAME;
        if (Name.Equals(string.Empty))
            return null;

        var Infos = TargetType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var Info in Infos)
        {
            if (!Info.Name.Equals(Name))
                continue;

            return Info;
        }

        return null;
    }

    private bool IsValidInfo(MethodInfo Info, bool bIsForCheckSuccess)
    {
        return Info.IsDefined(typeof(QuestableAttribute)) &&
            (bIsForCheckSuccess ? IsValidCheckSuccess(Info) : IsValidOnCompletion(Info));
    }


    private bool IsValidOnCompletion(MethodInfo Info)
    {
        return Info.ReturnType == typeof(void) && Info.GetParameters().Length == 0;
    }



    private static string REGISTER_NAME = "RegisterQuest";
    private static string DEREGISTER_NAME = "DeregisterQuest";
}

public abstract class Questable<T> : Questable
{
    public override void Init()
    {
        TrySetTriggerRegister();
        TrySetCallbacks();
    }

    public override void AddGenerics(Quest Quest)
    {
        Quest<T> QuestT = new(Quest);
        QuestT.CheckSuccess = GetCheckSuccessFunction();
        QuestT.AddCompletionCallback(GetCompletionCallback());
        QuestT.DeRegisterAction = GetRegisterCallback(false);
        QuestT.FollowUpQuest = FollowUpQuest;
        QuestT.QuestableID = ID;
        Quest.Add(QuestT);
        GetRegisterCallback(true).Invoke(QuestT);
    }

    public bool TryGetScriptType<U>(MonoScript QueryScript, out Type FoundType)
    {
        var List = TypeCache.GetTypesDerivedFrom<U>().ToList();
        foreach (var Element in List)
        {
            if (QueryScript == null)
                continue;

            if (QueryScript.GetClass() == null || !QueryScript.GetClass().IsEquivalentTo(Element))
                continue;

            FoundType = Element;
            return true;
        }

        FoundType = default;
        return false;
    }

    public override bool TrySetTriggerRegister()
    {
        return TryGetScriptType<IQuestTrigger<T>>(TriggerRegisterScript, out TriggerRegisterType);
    }

    public override bool TrySetCallbacks()
    {
        return TryGetScriptType<IQuestCallback>(CallbackScript, out CallbackType);
    }

    protected override bool IsValidCheckSuccess(MethodInfo Info)
    {
        return Info.ReturnType == typeof(int) && HasParameter(Info.GetParameters());
    }

    private bool HasParameter(ParameterInfo[] Infos)
    {
        foreach (ParameterInfo Info in Infos) {
            if (Info.ParameterType != typeof(T))
                continue;

            return true;
        }

        return false;
    }

    public Func<T, int> GetCheckSuccessFunction()
    {
        if (!Game.TryGetServiceByType(CallbackType, out GameService Service))
            return null;

        MethodInfo Info = GetInfoForCallback(true);
        if (Info == null)
            return null;

        var Data = Expression.Parameter(typeof(T), "Data");
        var Call = Expression.Call(Expression.Constant(Service), Info, Data);
        var Lambda = Expression.Lambda<Func<T, int>>(Call, Data);
        Func<T, int> Result = Lambda.Compile();

        return Result;
    }

    public Action GetCompletionCallback()
    {
        MethodInfo Info = GetInfoForCallback(false);
        if (!Game.TryGetServiceByType(CallbackType, out GameService Service))
            return null;

        Action Result = Expression.Lambda<Action>(
         Expression.Call(Expression.Constant(Service), Info)).Compile();

        return Result;
    }

    public Action<Quest<T>> GetRegisterCallback(bool bIsForRegister)
    {
        MethodInfo Info = GetStaticInfoForTrigger(bIsForRegister);
        if (Info == null)
            return null;

        var Data = Expression.Parameter(typeof(Quest<T>), "Data");
        Action<Quest<T>> Result = Expression.Lambda<Action<Quest<T>>>(
         Expression.Call(Info, Data), Data).Compile();
        return Result;
    }
}
