using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

public class NumberedIconScreen : SimpleIconScreen
{
    private TMPro.TextMeshProUGUI CountText;

    public override void Initialize(Sprite Sprite, bool bShowRegular)
    {
        base.Initialize(Sprite, bShowRegular);
        CountText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        float x = CountText.transform.localPosition.x;
        CountText.transform.localPosition = new Vector3(bShowRegular ? x : -x, 0, 0);
        CountText.alignment = bShowRegular ? TMPro.TextAlignmentOptions.MidlineLeft : TMPro.TextAlignmentOptions.MidlineRight;
    }

    public void UpdateVisuals(int Count)
    {
        CountText.text = "" + Count;
    }
}
