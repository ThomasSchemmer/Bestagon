using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbandonScreen : ScreenUI
{
    protected override void Initialize()
    {
        base.Initialize();

        if (Game.Instance.Mode != Game.GameMode.Game)
        {
            Hide();
        }
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
}
