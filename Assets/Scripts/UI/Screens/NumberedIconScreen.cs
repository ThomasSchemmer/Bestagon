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
    private Production.Type Type;
    private int Amount = -1;

    public void OnDestroy()
    {
        SetSubscription(false);
    }

    public override void Initialize(Sprite Sprite, string HoverTooltip, ISelectable Parent)
    {
        base.Initialize(Sprite, HoverTooltip, Parent);
        Initialize();
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
    }

    private void Initialize()
    {
        CountText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        float x = CountText.transform.localPosition.x;

        CountText.transform.localPosition = new Vector3(x, 0, 0);
        CountText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }

    public void SetAmountAlignment(TMPro.TextAlignmentOptions Alignment)
    {
        CountText.alignment = Alignment;
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

    public void SetSubscription(Production.Type Type, int Amount)
    {
        if (Amount < 0)
        {
            SetSubscription(false);
            return;
        }
        this.Type = Type;
        this.Amount = Amount;
        SetSubscription(true);
    }

    private void SetSubscription(bool bEnable)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        if (bEnable) 
        {
            Stockpile._OnResourcesChanged += UpdateColor;
            UpdateColor();
        }
        else
        {
            Stockpile._OnResourcesChanged -= UpdateColor;
        }
    }

    private void UpdateColor()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        bool bCanAfford = Stockpile.CanAfford(new Production(Type, Amount));
        CountText.color = bCanAfford ? ALLOWED_COLOR : FORBIDDEN_COLOR;
    }


    private static Color ALLOWED_COLOR = new Color(0.2f, 0.4f, 0.2f);
    private static Color FORBIDDEN_COLOR = new Color(0.9f, 0.25f, 0.25f);

}
