using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipGenerator : GameService
{
    private List<Tip> Tips;

    public void AddWorldTipFor(GameObject Target, string Text)
    {

    }

    protected override void StartServiceInternal() {
        Tips = new List<Tip>();
    }

    protected override void StopServiceInternal() {}

}
