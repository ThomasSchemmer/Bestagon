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
            case OnTurnBuildingEffect.Type.Produce:
                PrintYieldProduce(Position, Property, Label); break;
            case OnTurnBuildingEffect.Type.ConsumeProduce:
                PrintYieldConsumeProduce(Position, Property, Label); break;
            case OnTurnBuildingEffect.Type.ProduceUnit:
                PrintYieldProduceUnit(Position, Property, Label); break;
        }
    }

    private void PrintYieldProduce(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty ProductionProperty = Property.FindPropertyRelative("Production");
        SerializedProperty UpgradeProductionProperty = Property.FindPropertyRelative("UpgradeProduction");
        SerializedProperty RangeProperty = Property.FindPropertyRelative("Range");

        EditorGUILayout.BeginVertical("window");
        EditorGUILayout.PropertyField(ProductionProperty);
        EditorGUILayout.PropertyField(RangeProperty);
        EditorGUILayout.PropertyField(UpgradeProductionProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void PrintYieldProduceUnit(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty ConsumptionProperty = Property.FindPropertyRelative("Consumption");
        EditorGUILayout.BeginVertical("window");

        EditorGUILayout.PropertyField(ConsumptionProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void PrintYieldConsumeProduce(Rect Position, SerializedProperty Property, GUIContent Label)
    {
        SerializedProperty ConsumptionProperty = Property.FindPropertyRelative("Consumption");
        SerializedProperty ProductionProperty = Property.FindPropertyRelative("Production");

        EditorGUILayout.BeginVertical("window");

        EditorGUILayout.PropertyField(ConsumptionProperty);
        EditorGUILayout.PropertyField(ProductionProperty);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }


}