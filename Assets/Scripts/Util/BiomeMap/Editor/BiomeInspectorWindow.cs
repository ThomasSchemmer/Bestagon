using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Graphs;
using UnityEngine;

/**
 * Window to configure biome settings per drag and drop 
 * Automatically saves/loads to Assets/Scripts/Config/BiomeMap.asset
 */
public class BiomeInspectorWindow : EditorWindow
{
    private static Vector2 DragStart, Offset, OldOffset;
    BiomeMap Map;
    Biome Selected = null;

    private void OnEnable()
    {
        Styles.Init();

        string[] guids = AssetDatabase.FindAssets("t:biomeMap", new[] { "Assets/Scripts/Config/" });
        foreach (string guid in guids)
        {
            string Path = AssetDatabase.GUIDToAssetPath(guid);
            Map = (BiomeMap)AssetDatabase.LoadAssetAtPath(Path, typeof(BiomeMap));
        }
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (Map == null)
            return;

        EditorUtility.SetDirty(Map);
        AssetDatabase.SaveAssetIfDirty(Map);
        AssetDatabase.Refresh();
#endif
    }


    void OnGUI()
    {
        DrawBiomes();
        DrawTypeSelector();
        ProcessEvents(Event.current);
        if (GUI.changed)
        {
            Repaint();
        }
    }

    private void DrawTypeSelector()
    {
        if (Selected == null)
            return;

        string[] Names = Enum.GetNames(typeof(HexagonConfig.HexagonType));
        Selected.HexagonType = (HexagonConfig.HexagonType)EditorGUILayout.MaskField(
            "Tile",
            (int)Selected.HexagonType,
            Names
        );
    }

    private void DrawBiomes()
    {
        foreach (Biome Biome in Map.ClimateMap)
        {
            DrawBiome(Biome);
        }
    }

    private void DrawBiome(Biome Biome)
    {
        Rect Pos = GetOffset(Biome.Rect);

        GUI.Box(Pos, Biome.HexagonType.ToString(), Selected == Biome ? Styles.SelectedStyle : Styles.BoxStyle);
    }

    private void ProcessEvents(Event e)
    {
        if (HandleLeftClick(e))
        {
            e.Use();
            return;
        }
        if (HandleRightClick(e))
        {
            e.Use();
            return;
        }
        if (HandleDragging(e))
        {
            e.Use();
            return;
        }
    }

    private bool HandleLeftClick(Event e)
    {
        if (e.type != EventType.MouseDown || e.button != 0)
            return false;

        DragStart = e.mousePosition;

        foreach (Biome Biome in Map.ClimateMap)
        {
            if (Biome.Rect.Contains(e.mousePosition + Offset))
            {
                Selected = Biome;
                return true;
            }
        }
        //swallow ui input
        if (e.mousePosition.y > 25)
        {
            Selected = null;
        }
        return true;
    }

    private bool HandleRightClick(Event e)
    {
        if (e.type != EventType.MouseDown || e.button != 1)
            return false;

        Biome Biome = new Biome();
        Vector2 Pos = e.mousePosition + Offset;
        Biome.Rect = new Rect(Pos.x, Pos.y, 100, 50);
        Biome.HexagonType = 0;
        Map.AddBiome(Biome);

        return true;
    }

    private bool HandleDragging(Event e)
    {
        if (e.button != 2 && e.button != 0)
            return false;

        bool bIsDraggingElement = e.button == 0;
        if (bIsDraggingElement && Selected == null)
            return false;

        switch (e.type)
        {
            case EventType.MouseDown:
                DragStart = e.mousePosition;
                return true;
            case EventType.MouseDrag:
                if (bIsDraggingElement)
                {
                    Selected.Rect.position += e.mousePosition;
                    DragStart = e.mousePosition;
                }
                else
                {
                    Offset = DragStart - e.mousePosition + OldOffset;
                }
                return true;
            case EventType.MouseUp:
                OldOffset = Offset;
                return true;

        }
        return false;
    }

    public static Rect GetOffset(Rect Original)
    {
        return new Rect(Original.x - Offset.x,
            Original.y - Offset.y,
            Original.width,
            Original.height);
    }

    [MenuItem("Window/BiomeInspector")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BiomeInspectorWindow));
    }

    public class Styles
    {
        public static GUIStyle BoxStyle, SelectedStyle;

        public static void Init()
        {
            BoxStyle = new GUIStyle();
            BoxStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            BoxStyle.border = new RectOffset(12, 12, 12, 12);
            BoxStyle.alignment = TextAnchor.MiddleCenter;
            BoxStyle.normal.textColor = Color.white;

            SelectedStyle = new GUIStyle();
            SelectedStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/node1.png") as Texture2D;
            SelectedStyle.border = new RectOffset(12, 12, 12, 12);
            SelectedStyle.alignment = TextAnchor.MiddleCenter;
            SelectedStyle.normal.textColor = Color.white;

        }
    }
}
