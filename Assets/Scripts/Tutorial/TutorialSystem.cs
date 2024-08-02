using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSystem : GameService
{
    protected bool bIsInTutorial = false;
    protected GameObject Container;
    protected TMPro.TextMeshProUGUI TutorialText;

    protected TutorialType CurrentTutorial;
    protected int CurrentIndex = 0;

    protected RectTransform HighlightedRect;

    public SerializedDictionary<TutorialType, Tutorial> Tutorials = new();
    public Button ButtonNext, ButtonPrev, ButtonClose;



    public enum TutorialType : uint
    {
        Camera = 1 << 0,
        Tile = 1 << 1,


    }

    public bool IsInTutorial() {  return bIsInTutorial; }

    public void SetInTutorial(bool bInTutorial)
    {
        bIsInTutorial = bInTutorial;
    }

    public void OnDisplayNext()
    {
        int MaxIndex = Tutorials[CurrentTutorial].GetMaxIndex();
        CurrentIndex = Mathf.Min(MaxIndex, CurrentIndex + 1);
        Display();
    }

    public void OnDisplayPrev()
    {
        CurrentIndex = Mathf.Max(CurrentIndex - 1, 0);
        Display();
    }

    public void DisplayTextFor(TutorialType Type) {
        Show(true);
        CurrentTutorial = Type;
        CurrentIndex = 0;
        Display();
    }

    protected void Display()
    {
        ShowInterface();
        if (!Tutorials.ContainsKey(CurrentTutorial))
            return;

        Tutorial Current = Tutorials[CurrentTutorial];
        if (!Current.IsValidIndex(CurrentIndex))
            return;

        Tutorials[CurrentTutorial].Display(Container, CurrentIndex);
        ButtonPrev.interactable = Current.IsValidIndex(CurrentIndex - 1);
        ButtonNext.interactable = Current.IsValidIndex(CurrentIndex + 1);

    }

    protected override void StartServiceInternal() {
        Container = transform.GetChild(0).gameObject;
        Container.SetActive(false);
        TutorialText = Container.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        HighlightedRect = transform.GetChild(1).GetComponent<RectTransform>();

        if (IsInTutorial())
        {
            DisplayTextFor(TutorialType.Camera);
        }

        _OnInit?.Invoke(this);
    }

    private void ShowInterface()
    {
        Game.RunAfterServiceInit((QuestService QuestService) =>
        {
            bool bShouldShow = CurrentTutorial > TutorialType.Camera ||
                (CurrentTutorial == TutorialType.Camera && CurrentIndex > 1);
            QuestService.Show(bShouldShow);
        });
        Game.RunAfterServiceInit((Turn Turn) =>
        {
            Turn.Show(false);
        });
        Game.RunAfterServiceInit((Stockpile Stockpile) =>
        {
            Stockpile.Show(false);
        });
        Game.RunAfterServiceInit((CardDeck CardDeck) =>
        {
            CardDeck.Show(false);
        });
        Game.RunAfterServiceInit((DiscardDeck DiscardDeck) =>
        {
            DiscardDeck.Show(false);
        });

        bool bShouldShow = CurrentTutorial > TutorialType.Camera ||
            (CurrentTutorial == TutorialType.Camera && CurrentIndex > 1);
        ButtonClose.gameObject.SetActive(bShouldShow);
    }

    public void OnClose()
    {
        Show(false);
    }

    public void Show(bool bShow)
    {
        Container.SetActive(bShow);
    }

    public void Highlight(RectTransform Rect)
    {
        if (Rect != null)
        {
            HighlightedRect.gameObject.SetActive(true);
            HighlightedRect.position = Rect.position;
            HighlightedRect.sizeDelta = Rect.sizeDelta;
        }
        else{
            HighlightedRect.gameObject.SetActive(false);
        }
    }


    protected override void StopServiceInternal() {}

}
