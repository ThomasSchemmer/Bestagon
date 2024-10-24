using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbandonScreen : ScreenUI
{
    protected NumberedIconScreen UpgradesScreen;

    protected override void Initialize()
    {
        base.Initialize();

        InitUpgradeVisuals();

        if (Game.Instance.Mode != Game.GameMode.Game)
        {
            Hide();
        }
    }

    private void InitUpgradeVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject UpgradesVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Upgrades, null, 0, false, true);
        this.UpgradesScreen = UpgradesVisuals.GetComponent<NumberedIconScreen>();
        this.UpgradesScreen.HoverTooltip = "Upgrade points";
        RectTransform UpgradesScreen = UpgradesVisuals.GetComponent<RectTransform>();
        UpgradesScreen.SetParent(Container.transform, false);
        UpgradesScreen.anchoredPosition = new(32, -45);


        Game.RunAfterServicesInit((Stockpile Stockpile, IconFactory IconFactory) =>
        {
            Stockpile._OnUpgradesChanged += UpdateUpgradesVisuals;
        });
    }

    public void OnClick()
    {
        ConfirmScreen.Show("Are you sure you want to abandon your tribe?", OnAbandonRun);
    }

    private void OnAbandonRun()
    {
        if (!Game.TryGetService(out Turn Turn))
            return;

        Turn.InvokeOnRunAbandoned();
        Game.Instance.GameOver("You have abandoned your current tribe!");
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
    }

    private void UpdateUpgradesVisuals()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        UpgradesScreen.UpdateVisuals(Stockpile.GetUpgradePoints());
    }

    private void OnDestroy()
    {
        Stockpile._OnUpgradesChanged -= UpdateUpgradesVisuals;
    }
}
