using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

/** 
 * Class representing an icon screen that also contains a number, eg indicating its amount
 */
public class NumberedIconScreen : SimpleIconScreen
{
    private TMPro.TextMeshProUGUI CountText;

    public override void Initialize(Sprite Sprite, bool bShowRegular, string HoverTooltip)
    {
        base.Initialize(Sprite, bShowRegular, HoverTooltip);
        Initialize(bShowRegular);
    }

    public override void Initialize(Sprite Sprite, bool bShowRegular, ISelectable Parent)
    {
        base.Initialize(Sprite, bShowRegular, Parent);
        Initialize(bShowRegular);
    }

    private void Initialize(bool bShowRegular)
    {
        CountText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        float x = CountText.transform.localPosition.x;
        CountText.transform.localPosition = new Vector3(bShowRegular ? x : -x, 0, 0);
        CountText.alignment = bShowRegular ? TMPro.TextAlignmentOptions.MidlineLeft : TMPro.TextAlignmentOptions.MidlineRight;
    }

    public void UpdateVisuals(int Count, int Max = -1)
    {
        string MaxText = Max >= 0 ? "/" + Max : "";
        CountText.text = "" + Count + MaxText;
    }
}
