using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/** 
 * Custom display for any questable
 * Useful for automatic verification and more importantly dropdown function selection
 * using reflection and attributes
 */

[CustomEditor(typeof(Questable))]
public class QuestableEditor : Editor
{
    override public void OnInspectorGUI()
    {
        Questable Questable = target as Questable;

        DrawRegularProperties();
        GUILayout.Space(10);
        
        DrawTriggerRegisterField();
        GUILayout.Space(10);

        bool bIsDisabled = !Verify(Questable);

        EditorGUILayout.BeginVertical();
        DrawCallbackField();
        DrawCheckSuccess(Questable, bIsDisabled);
        DrawOnCompletion(Questable, bIsDisabled);
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        DrawUnlockField();
        DrawOnUnlock(Questable);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRegularProperties()
    {

        SerializedProperty StartProgressProp = serializedObject.FindProperty("StartProgress");
        SerializedProperty MaxProgressProp = serializedObject.FindProperty("MaxProgress");
        SerializedProperty DescriptionProp = serializedObject.FindProperty("Description");
        SerializedProperty QuestTypeProp = serializedObject.FindProperty("QuestType");
        SerializedProperty IDProp = serializedObject.FindProperty("ID");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Start Progress: ");
        StartProgressProp.intValue = EditorGUILayout.DelayedIntField(StartProgressProp.intValue, GUILayout.MaxWidth(MAX_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("End Progress: ");
        MaxProgressProp.intValue = EditorGUILayout.DelayedIntField(MaxProgressProp.intValue, GUILayout.MaxWidth(MAX_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Description: ");
        DescriptionProp.stringValue = EditorGUILayout.DelayedTextField(DescriptionProp.stringValue, GUILayout.MaxWidth(MAX_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Type: ");
        string[] QuestTypes = Enum.GetNames(typeof(Quest.Type));
        QuestTypeProp.intValue = EditorGUILayout.Popup(QuestTypeProp.intValue, QuestTypes, GUILayout.MaxWidth(MAX_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("ID: ");
        IDProp.intValue = EditorGUILayout.DelayedIntField(IDProp.intValue, GUILayout.MaxWidth(MAX_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        SerializedProperty RegisterProp = serializedObject.FindProperty("Sprite");
        EditorGUILayout.ObjectField(RegisterProp, typeof(Sprite));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        SerializedProperty FollowUpProp = serializedObject.FindProperty("FollowUpQuest");
        EditorGUILayout.ObjectField(FollowUpProp, typeof(Questable));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTriggerRegisterField()
    {
        SerializedProperty RegisterProp = serializedObject.FindProperty("RegisterScript");
        EditorGUILayout.ObjectField(RegisterProp, typeof(MonoScript));
    }
    private void DrawCallbackField()
    {
        SerializedProperty RegisterProp = serializedObject.FindProperty("CallbackScript");
        EditorGUILayout.ObjectField(RegisterProp, typeof(MonoScript));
    }

    private void DrawUnlockField()
    {
        SerializedProperty UnlockProp = serializedObject.FindProperty("UnlockScript");
        EditorGUILayout.ObjectField(UnlockProp, typeof(MonoScript));
    }

    private void DrawCheckSuccess(Questable Questable, bool bIsDisabled)
    {
        EditorGUI.BeginDisabledGroup(bIsDisabled);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("CheckSuccess: ");
        string[] AvailableMethods = Questable.GetQuestableMethods(Questable.ScriptType.CallbackCheckSuccess);
        SerializedProperty CheckSuccessProperty = serializedObject.FindProperty("CheckSuccessName");
        int Selected = AvailableMethods.ToList().IndexOf(CheckSuccessProperty.stringValue);
        Selected = EditorGUILayout.Popup(Selected, AvailableMethods, GUILayout.MaxWidth(MAX_WIDTH));
        CheckSuccessProperty.stringValue = Selected < 0 ? "" : AvailableMethods[Selected];
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    private void DrawOnCompletion(Questable Questable, bool bIsDisabled)
    {
        EditorGUI.BeginDisabledGroup(bIsDisabled);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("OnCompletion: ");
        string[] AvailableMethods = Questable.GetQuestableMethods(Questable.ScriptType.CallbackCompletion);
        SerializedProperty OnCompletionProperty = serializedObject.FindProperty("OnCompletionName");
        int Selected = AvailableMethods.ToList().IndexOf(OnCompletionProperty.stringValue);
        Selected = EditorGUILayout.Popup(Selected, AvailableMethods, GUILayout.MaxWidth(MAX_WIDTH));
        OnCompletionProperty.stringValue = Selected < 0 ? "" : AvailableMethods[Selected];
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    private void DrawOnUnlock(Questable Questable)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Unlocks with: ");
        string[] AvailableMethods = Questable.GetQuestableMethods(Questable.ScriptType.Unlock);
        SerializedProperty UnlockProperty = serializedObject.FindProperty("UnlockName");
        int Selected = AvailableMethods.ToList().IndexOf(UnlockProperty.stringValue);
        Selected = EditorGUILayout.Popup(Selected, AvailableMethods, GUILayout.MaxWidth(MAX_WIDTH));
        UnlockProperty.stringValue = Selected < 0 ? "" : AvailableMethods[Selected];
        EditorGUILayout.EndHorizontal();
    }

    private bool Verify(Questable Questable)
    {
        if (!Questable.TrySetRegister())
        {
            Debug.LogError("Register Script has to inherit from IQuestTrigger!");
            Questable.RegisterScript = null;
            return false;
        }

        if (!Questable.TrySetCallbacks())
        {
            Debug.LogError("Callback Script has to inherit from IQuestCallback!");
            Questable.CallbackScript = null;
            return false;
        }

        if (Questable.UnlockScript != null && !Questable.TrySetOnUnlock())
        {
            Debug.LogError("Unlock Script has to inherit from IQuestUnlock!");
            Questable.UnlockScript = null;
            return false;
        }


        return true;
    }

    private static int MAX_WIDTH = 300;
}
