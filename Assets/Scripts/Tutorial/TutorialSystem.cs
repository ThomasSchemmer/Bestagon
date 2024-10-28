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

    public SerializedDictionary<TutorialType, Tutorial> Tutorials = new();
    public Button ButtonNext, ButtonPrev, ButtonClose;



    public enum TutorialType : uint
    {
        Camera = 1 << 0,
        Tile = 1 << 1,
        Resources = 1 << 2,
        Cards = 1 << 3,
        Buildings = 1 << 4,
        Workers = 1 << 5,
        Turns = 1 << 6,
        Scouts = 1 << 7,
        Malaise = 1 << 8,
        AbandonRun = 1 << 9,

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
        Game.RunAfterServicesInit((Turn Turn, RelicService RelicService) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.Turns;
            Turn.Show(bShouldShow);

            AbandonScreen Screen = Turn.GetAbandonScreen();
            bool bShouldShowAbandon = CurrentTutorial >= TutorialType.AbandonRun;
            Screen.Show(bShouldShowAbandon);

            bool bShouldShowRelics = CurrentTutorial >= TutorialType.AbandonRun;
            RelicService.ShowRelicButton.SetActive(bShouldShowRelics);
        });
        Game.RunAfterServiceInit((Stockpile Stockpile) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.Resources;
            Stockpile.Show(bShouldShow);
        });
        Game.RunAfterServiceInit((CardHand CardHand) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.Cards;
            CardHand.Show(bShouldShow);
        });
        Game.RunAfterServiceInit((CardDeck CardDeck) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.Turns;
            CardDeck.Show(bShouldShow);
        });
        Game.RunAfterServiceInit((DiscardDeck DiscardDeck) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.Turns;
            DiscardDeck.Show(bShouldShow);
        });
        Game.RunAfterServiceInit((CardGroupManager CardGroupManager) =>
        {
            bool bShouldShow = CurrentTutorial >= TutorialType.AbandonRun;
            CardGroupManager.CardGroupsScreen.DisplayScreenButton(bShouldShow);
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
        if (!IsInit)
            return;
        Container.SetActive(bShow);
    }


    protected override void StopServiceInternal() {}

}
