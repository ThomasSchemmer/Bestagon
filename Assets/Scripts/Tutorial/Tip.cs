using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tip", menuName = "ScriptableObjects/Tip", order = 3)]
public class Tip : ScriptableObject
{
    public string Text;
    public GameObject Target;
    public bool bWasShown = false;
    public bool bIsUI = false;

    private RectTransform TipTransform;
    private TMPro.TextMeshProUGUI TextField;

    public void Display(GameObject TipObject)
    {
        ParseUIObject(TipObject);
        TipTransform.localPosition = GetScreenPos();
        TextField.text = Text;
    }

    private Vector2 GetScreenPos()
    {
        Vector2 TargetPos;
        if (Target == null)
        {
            TargetPos = new(Screen.width / 2f, Screen.height / 2f);
        }
        else
        {
            TargetPos = bIsUI ? GetScreenPosUI() : GetScreenPosWorld();
        }
        TargetPos -= TipTransform.sizeDelta / 2f;
        return TargetPos;
    }

    private Vector2 GetScreenPosUI()
    {
        RectTransform TargetTransform = Target.GetComponent<RectTransform>();
        return new Vector2(TargetTransform.localPosition.x, TargetTransform.localPosition.y) + TargetTransform.sizeDelta;
    }

    private Vector2 GetScreenPosWorld()
    {
        return Camera.main.WorldToScreenPoint(Target.transform.position);
    }

    private void ParseUIObject(GameObject TipObject)
    {
        TipTransform = TipObject.GetComponent<RectTransform>();
        TextField = TipObject.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
    }
}
