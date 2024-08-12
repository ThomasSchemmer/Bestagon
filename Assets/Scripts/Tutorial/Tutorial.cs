using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Tutorial", menuName = "ScriptableObjects/Tutorial", order = 3)]
public class Tutorial : ScriptableObject
{
    public string[] Texts;
    public Vector2[] Positions;

    private TMPro.TextMeshProUGUI TextField;

    public void Display(GameObject TipObject, int i)
    {
        if (TipObject == null)
            return;

        ParseUIObject(TipObject);
        TipObject.SetActive(true);
        TextField.text = Parse(Texts[i]);
        Vector2 Position = new Vector2(
            Positions[i].x * Screen.width / 2,
            Positions[i].y * Screen.height / 2
        );
        TipObject.GetComponent<RectTransform>().anchoredPosition = Position;
    }

    private string Parse(string Original)
    {
        string[] Texts = Original.Split("\\n");
        string Output = Texts[0];
        for (int i = 1; i < Texts.Length; i++)
        {
            Output += "\n"+Texts[i];
        }
        return Output;
    }

    private void ParseUIObject(GameObject TipObject)
    {
        if (TipObject == null) 
            return;

        TextField = TipObject.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
    }

    public bool IsValidIndex(int Index)
    {
        if (Index < 0)
            return false;

        return Texts.Length > Index;
    }

    public int GetMaxIndex()
    {
        return Texts.Length;
    }
}
