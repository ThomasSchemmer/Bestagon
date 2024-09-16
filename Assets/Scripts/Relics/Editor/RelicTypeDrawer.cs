using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(RelicType))]
public class RelicTypeDrawer : PropertyDrawer
{

    public override void OnGUI(Rect Position, SerializedProperty RelicTypeProperty, GUIContent Label)
    {
        string[] RelicTypes = Enum.GetNames(typeof(RelicType));
        RelicTypeProperty.intValue = EditorGUILayout.MaskField(
            "Type",
            RelicTypeProperty.intValue,
            RelicTypes
        );
    }
}
