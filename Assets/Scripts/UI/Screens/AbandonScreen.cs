using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbandonScreen : ScreenUI
{
    public void OnClick()
    {
        Action A = () =>
        {
            Game.Instance.GameOver("You have abandoned your current tribe!");
        };
        ConfirmScreen.Show("Are you sure you want to abandon your tribe?", A);
    }
}
