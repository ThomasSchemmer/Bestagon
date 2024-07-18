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
    public MonoScript RegisterScript;
    /** Script that will be called on certain quest events, eg check for fulfillment */
    public MonoScript CallbackScript;
    /** Script that triggers the unlocking of the quest (and its subsequently adding to the system)*/
    public MonoScript UnlockScript;

    public Type RegisterType;
    public Type CallbackType;
    public Type UnlockType;
    public string CheckSuccessName;
    public string OnCompletionName;
    public string UnlockName;

    public abstract bool TrySetRegister();
    public abstract bool TrySetCallbacks();
    public abstract bool TrySetOnUnlock();
    public abstract void Init();
    public abstract void AddGenerics(Quest Quest);
    protected abstract bool IsValidCheckSuccess(MethodInfo Info);

    public enum ScriptType
    {
        Trigger,
        CallbackCheckSuccess,
        CallbackCompletion, 
        Unlock
    }

    public string[] GetQuestableMethods(ScriptType Type)
    {
        string[] List = new string[0];
        Type TargetType = GetTargetType(Type);
        if (TargetType == null)
            return List;

        var Infos = TargetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic)
                    .Where(_ => IsValidInfo(_, Type));
        List = new string[Infos.Count()];
        for (int i = 0; i < Infos.Count(); i++)
        {
            List[i] = Infos.ElementAt(i).Name;
        }
        return List;
    }

    private Type GetTargetType(ScriptType Type)
    {
        switch (Type)
        {
            case ScriptType.Trigger: return RegisterType;
            case ScriptType.CallbackCheckSuccess: return CallbackType;
            case ScriptType.CallbackCompletion: return CallbackType;
            case ScriptType.Unlock: return UnlockType;
        }
        return default;
    }

    private string GetFunctionName(ScriptType Type)
    {
        switch (Type)
        {
            case ScriptType.Trigger: return "";
            case ScriptType.CallbackCheckSuccess: return CheckSuccessName;
            case ScriptType.CallbackCompletion: return OnCompletionName;
            case ScriptType.Unlock: return UnlockName;
            default: return "";
        }
    }

    public MethodInfo GetInfoForCallback(ScriptType Type)
    {
        Type TargetType = GetTargetType(Type);
        if (TargetType == null)
            return null;

        string Name = GetFunctionName(Type);
        if (Name.Equals(string.Empty))
            return null;

        var Infos = TargetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic)
                    .Where(_ => IsValidInfo(_, Type));

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
        Type TargetType = RegisterType;
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

    private bool IsValidInfo(MethodInfo Info, ScriptType Type)
    {
        bool bIsValid = false;
        switch (Type)
        {
            case ScriptType.Unlock: bIsValid = IsValidOnUnlock(Info); break;
            case ScriptType.CallbackCheckSuccess: bIsValid = IsValidCheckSuccess(Info); break;
            case ScriptType.CallbackCompletion: bIsValid = IsValidOnCompletion(Info); break;
            default: bIsValid = true; break;
        }
        return bIsValid && Info.IsDefined(typeof(QuestableAttribute));
    }


    private bool IsValidOnCompletion(MethodInfo Info)
    {
        return Info.ReturnType == typeof(void) && Info.GetParameters().Length == 0;
    }

    private bool IsValidOnUnlock(MethodInfo Info)
    {
        return Info.ReturnType == typeof(bool) && Info.GetParameters().Length == 0;
    }



    private static string REGISTER_NAME = "RegisterQuest";
    private static string DEREGISTER_NAME = "DeregisterQuest";
}

public abstract class Questable<T> : Questable
{
    public override void Init()
    {
        TrySetRegister();
        TrySetCallbacks();
        TrySetOnUnlock();
    }

    public override void AddGenerics(Quest Quest)
    {
        Quest<T> QuestT = new(Quest);
        QuestT.CheckSuccess = GetCheckSuccessFunction();
        QuestT.AddCompletionCallback(GetCompletionCallback());
        QuestT.DeRegisterAction = GetRegisterCallback(false);
        QuestT.Unlock = GetUnlockCallback();
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

    public override bool TrySetRegister()
    {
        return TryGetScriptType<IQuestTrigger<T>>(RegisterScript, out RegisterType);
    }

    public override bool TrySetCallbacks()
    {
        return TryGetScriptType<IQuestCallback>(CallbackScript, out CallbackType);
    }

    public override bool TrySetOnUnlock()
    {
        return TryGetScriptType<IQuestUnlock>(UnlockScript, out UnlockType);
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

        MethodInfo Info = GetInfoForCallback(ScriptType.CallbackCheckSuccess);
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
        MethodInfo Info = GetInfoForCallback(ScriptType.CallbackCompletion);
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

    public Func<bool> GetUnlockCallback()
    {
        MethodInfo Info = GetInfoForCallback(ScriptType.Unlock);
        if (!Game.TryGetServiceByType(UnlockType, out GameService Service))
            return null;

        Func<bool> Result = Expression.Lambda<Func<bool>>(
         Expression.Call(Expression.Constant(Service), Info)).Compile();

        return Result;
    }
}
