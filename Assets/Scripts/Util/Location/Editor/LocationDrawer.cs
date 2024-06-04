using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(Location))]
public class LocationDrawer : PropertyDrawer
{
    public override void OnGUI(Rect Position, SerializedProperty LocationProperty, GUIContent Label)
    {
        SerializedProperty ChunkLocationProperty = LocationProperty.FindPropertyRelative("_ChunkLocation");
        SerializedProperty HexLocationProperty = LocationProperty.FindPropertyRelative("_HexLocation");
        SerializedProperty ChunkXProperty = ChunkLocationProperty.FindPropertyRelative("x");
        SerializedProperty ChunkYProperty = ChunkLocationProperty.FindPropertyRelative("y");
        SerializedProperty HexXProperty = HexLocationProperty.FindPropertyRelative("x");
        SerializedProperty HexYProperty = HexLocationProperty.FindPropertyRelative("y");

        Rect CurrentPosition = Position;
        CurrentPosition.width = (Position.width - 80) / 4.0f;

        EditorGUI.BeginProperty(Position, Label, LocationProperty);

        DrawIntField(ChunkXProperty, ref CurrentPosition, "X");
        DrawIntField(ChunkYProperty, ref CurrentPosition, "Y");
        DrawIntField(HexXProperty, ref CurrentPosition, "X");
        DrawIntField(HexYProperty, ref CurrentPosition, "Y");

        EditorGUI.EndProperty();  
    }

    private void DrawIntField(SerializedProperty Property, ref Rect Position, string Label)
    {
        EditorGUI.PrefixLabel(Position, new(Label));
        Position.x += 15;
        Property.intValue = EditorGUI.DelayedIntField(Position, Property.intValue);
        Position.x += Position.width + 5;
    }

}