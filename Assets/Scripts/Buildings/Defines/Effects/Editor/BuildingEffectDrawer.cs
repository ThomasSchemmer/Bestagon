using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


[CustomPropertyDrawer(typeof(OnTurnBuildingEffect))]
public class BuildingEffectDrawer : PropertyDrawer
{
    
    public override void OnGUI(Rect Position, SerializedProperty BuildingEffectProperty, GUIContent Label)
    {
        SerializedProperty EffectProperty = BuildingEffectProperty.FindPropertyRelative("EffectType");

        string[] Effects = Enum.GetNames(typeof(OnTurnBuildingEffect.Type));
        EditorGUI.BeginProperty(Position, Label, EffectProperty);
        EffectProperty.intValue = EditorGUILayout.Popup(
            "Effect",
            EffectProperty.intValue,
            Effects
        );
        EditorGUI.EndProperty();
        Position.y += EditorGUI.GetPropertyHeight(EffectProperty, Label, true);
        OnTurnBuildingEffect.Type SelectedEffect = (OnTurnBuildingEffect.Type)EffectProperty.intValue;
        
        PrintPropertiesForEffect(Position, BuildingEffectProperty, Label, SelectedEffect);

    }

    private void PrintPropertiesForEffect(Rect Position, SerializedProperty Property, GUIContent Label, OnTurnBuildingEffect.Type EffectType)
    {
        switch (EffectType)
        {
            case OnTurnBuildingEffect.Type.YieldPerWorker:
                PrintYieldPerWorker(Position, Property, Label); break;
            case OnTurnBuildingEffect.Type.YieldPerAreaAndWorker:
                PrintYieldPerAreaAndWorker(Position, Property, Label); break;
            case OnTurnBuildingEffect.Type.YieldWorkerPerWorker:
                PrintYieldWorkerPerWorker(Position, Property, Label); break;
            case OnTurnBuildingEffect.Type.IncreaseYield:
                PrintIncreaseYield(Position, Property, Label); break;
        }
    }

    private void PrintYieldPerWorker(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty TileTypeProperty = Property.FindPropertyRelative("TileType");
        SerializedProperty ProductionProperty = Property.FindPropertyRelative("Production");
       
        EditorGUILayout.BeginVertical("window");
        string[] Hexagons = Enum.GetNames(typeof(HexagonConfig.HexagonType));
        TileTypeProperty.intValue = EditorGUILayout.MaskField(
            "Tile",
            TileTypeProperty.intValue,
            Hexagons
        );
        EditorGUILayout.PropertyField(ProductionProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void PrintYieldPerAreaAndWorker(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty TileTypeProperty = Property.FindPropertyRelative("TileType");
        SerializedProperty ProductionProperty = Property.FindPropertyRelative("Production");
        SerializedProperty RangeProperty = Property.FindPropertyRelative("Range");
        
        EditorGUILayout.BeginVertical("window");
        string[] Hexagons = Enum.GetNames(typeof(HexagonConfig.HexagonType));
        TileTypeProperty.intValue = EditorGUILayout.MaskField(
            "Tile",
            TileTypeProperty.intValue,
            Hexagons
        );
        EditorGUILayout.PropertyField(ProductionProperty);
        EditorGUILayout.PropertyField(RangeProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void PrintYieldWorkerPerWorker(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        // Nothing is needed
    }

    private void PrintIncreaseYield(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty RangeProperty = Property.FindPropertyRelative("Range");
        SerializedProperty BuildingTypeProperty = Property.FindPropertyRelative("BuildingType");
        SerializedProperty ProductionIncreaseProperty = Property.FindPropertyRelative("ProductionIncrease");

        EditorGUILayout.BeginVertical("window");
        string[] Buildings = Enum.GetNames(typeof(BuildingData.Type));
        BuildingTypeProperty.intValue = EditorGUILayout.MaskField(
            "Building",
            BuildingTypeProperty.intValue,
            Buildings
        );
        EditorGUILayout.PropertyField(RangeProperty);
        EditorGUILayout.PropertyField(ProductionIncreaseProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }


}