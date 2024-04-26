using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipSystem : GameService
{
    public List<Tip> Tips = new();
    public GameObject TipPrefab;

    public void DisplayTip()
    {
        foreach (var Tip in Tips)
        {
            if (Tip.bWasShown)
                continue;

            DisplayTip(Tip);
            return;
        }
    }

    private void DisplayTip(Tip Tip)
    {
    }

    protected override void StartServiceInternal() {
    }

    protected override void StopServiceInternal() {}

}
