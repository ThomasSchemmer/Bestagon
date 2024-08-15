using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class BuildWindows
{
    [MenuItem("Builds/Windows Build With Postprocess")]
    public static void BuildGame()
    {
        string ExeName = "Bestangon.exe";
        string Path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "Builds", "") + "/";
        string[] levels = new string[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Main.unity", "Assets/Scenes/CardSelection.unity" };

        BuildPipeline.BuildPlayer(levels, Path + ExeName, BuildTarget.StandaloneWindows, BuildOptions.None);

        string TutorialPath = SaveGameManager.GetSavegamePath();
        string TutorialName = SaveGameManager.GetTutorialSave();
        FileUtil.DeleteFileOrDirectory(Path + TutorialName);
        FileUtil.CopyFileOrDirectory(TutorialPath + TutorialName, Path + TutorialName);

        //Process proc = new Process();
        //proc.StartInfo.FileName = Path + ExeName;
        //proc.Start();
    }

}
