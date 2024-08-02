using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Tutorial", menuName = "ScriptableObjects/Tutorial", order = 3)]
public class Tutorial : ScriptableObject
{
    public string[] Texts;
    public Vector2[] Positions;
    public MonoScript[] Highlights;

    private TMPro.TextMeshProUGUI TextField;

    public void Display(GameObject TipObject, int i)
    {
        ParseUIObject(TipObject);
        TipObject.SetActive(true);
        TextField.text = Parse(Texts[i]);
        Vector2 Position = new Vector2(
            Positions[i].x * Screen.width / 2,
            Positions[i].y * Screen.height / 2
        );
        TipObject.GetComponent<RectTransform>().anchoredPosition = Position;

        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;

        System.Type TargetType = Highlights[i] != null ? Highlights[i].GetClass() : null;
        Game.TryGetServiceByType(TargetType, out GameService TargetService);
        RectTransform TargetRect = TargetService != null ? TargetService.GetComponent<RectTransform>() : null;
        TutorialSystem.Highlight(TargetRect);
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
