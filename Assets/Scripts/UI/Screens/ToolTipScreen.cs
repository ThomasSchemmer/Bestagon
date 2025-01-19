using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipScreen : ScreenUI, UIElement
{
    protected override void Initialize()
    {
        base.Initialize();
        bIsInit = true;
    }

    public override void Hide()
    {
        if (!bIsInit)
            return;

        base.Hide();
    }

    public override void Show()
    {
        if (!bIsInit)
            return;

        base.Show();
    }

    public void Show(ISelectable Selectable, bool bShow)
    {
        if ((!bShow || Selectable == null))
        {
            Hide();
            return;
        }

        Show();
        string Tooltip = Selectable.GetHoverTooltip();
        int LineCount = Tooltip.Split("\n").Length;
        // reset to get accurate text sizes
        TextRect.sizeDelta = new Vector2(MaxWidth, HeightPerLine * LineCount);
        Text.text = Tooltip;
        Text.ForceMeshUpdate();
        Vector2 TextSize = Text.textBounds.size;
        int TextLength = Tooltip.Length;

        int Lines = TextLength / MaxCountPerLine + LineCount;
        int Height = HeightPerLine * Lines;
        float Width = TextLength > MaxCountPerLine ? MaxWidth : TextSize.x;
        TextRect.sizeDelta = new Vector2(Width, Height);
        BackgroundRect.sizeDelta = new Vector2(Width + 10, Height + 5);
        RectTransform ContainerRect = Container.GetComponent<RectTransform>();
        ContainerRect.anchoredPosition = new Vector2(Width / 2, -(Height + 5) / 2.0f);

        // flip if we are too far off screen
        Vector2 TargetPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y) + CursorOffset;
        TargetPosition.x -= TargetPosition.x + Width > Screen.width ? Width : 0;
        TargetPosition.y -= (Screen.height - TargetPosition.y) + Height > Screen.height ? Height : 0;
        SelfRect.anchoredPosition = TargetPosition;
    }

    protected override bool CountsAsPopup()
    {
        return false;
    }

    public void SetSelected(bool Selected) {}

    public void SetHovered(bool Hovered) { }

    public void ClickOn(Vector2 PixelPos) { }

    public void Interact() { }

    public bool IsEqual(ISelectable other)
    {
        if (other == null)
            return false;

        if (other is not ToolTipScreen)
            return false;

        return name.Equals((other as ToolTipScreen).name);
    }

    public bool CanBeLongHovered() { return false; }

    public bool CanBeInteracted() { return false; }
    public bool ShouldBeIgnored() { return true; }

    public RectTransform TextRect, BackgroundRect, SelfRect;
    public TMPro.TextMeshProUGUI Text;

    private bool bIsInit = false;

    public static int MaxCountPerLine = 40;
    public static int MaxWidth = 290;
    public static int HeightPerLine = 25;
    public static Vector2 CursorOffset = new(20, 0);
}
