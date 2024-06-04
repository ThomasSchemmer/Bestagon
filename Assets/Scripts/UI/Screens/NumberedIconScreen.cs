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

    public override void Initialize(Sprite Sprite, string HoverTooltip, ISelectable Parent)
    {
        base.Initialize(Sprite, HoverTooltip, Parent);
        Initialize();
    }

    private void Initialize()
    {
        CountText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        float x = CountText.transform.localPosition.x;

        CountText.transform.localPosition = new Vector3(x, 0, 0);
        CountText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }

    public void UpdateVisuals(int Count, int Max = -1)
    {
        string MaxText = Max >= 0 ? "/" + Max : "";
        CountText.text = "" + Count + MaxText;
    }

    public override void SetSelectionEnabled(bool bEnabled)
    {
        base.SetSelectionEnabled(bEnabled);
        CountText.gameObject.layer = bEnabled ? LayerMask.NameToLayer(Selectors.UILayerName) : 0;
    }
}
