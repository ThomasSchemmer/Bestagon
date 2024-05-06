using UnityEditor;

/** force unity to not use the abbreviated display of the Button, as it hides the hover tooltip
 */ 
[CustomEditor(typeof(HoverableButton))]
public class HoverableButtonEditor : Editor
{
    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
