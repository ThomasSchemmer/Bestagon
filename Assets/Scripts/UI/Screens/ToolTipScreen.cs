using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipScreen : MonoBehaviour
{
    public void Show(ISelectable Selectable, bool bShow)
    {
        if ((!bShow || Selectable == null))
        {
            Hide();
            return;
        }

        ContainerRect.gameObject.SetActive(true);
        // reset to get accurate text sizes
        TextRect.sizeDelta = new Vector2(MaxWidth, HeightPerLine);
        Text.text = Selectable.GetHoverTooltip();
        Text.ForceMeshUpdate();
        Vector2 TextSize = Text.textBounds.size;
        int TextLength = Selectable.GetHoverTooltip().Length;

        int Lines = TextLength / MaxCountPerLine + 1;
        int Height = HeightPerLine * Lines;
        float Width = TextLength > MaxCountPerLine ? MaxWidth : TextSize.x;
        TextRect.sizeDelta = new Vector2(Width, Height);
        BackgroundRect.sizeDelta = new Vector2(Width + 10, Height + 5);
        ContainerRect.anchoredPosition = new Vector2(Width / 2, -(Height + 5) / 2.0f);

        // flip if we are too far off screen
        Vector2 TargetPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y) + CursorOffset;
        TargetPosition.x -= TargetPosition.x + Width > Screen.width ? Width : 0;
        TargetPosition.y -= (Screen.height - TargetPosition.y) + Height > Screen.height ? Height : 0;
        SelfRect.anchoredPosition = TargetPosition;
    }

    public void Hide()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }

    public RectTransform TextRect, BackgroundRect, ContainerRect, SelfRect;
    public TMPro.TextMeshProUGUI Text;

    public static int MaxCountPerLine = 40;
    public static int MaxWidth = 290;
    public static int HeightPerLine = 25;
    public static Vector2 CursorOffset = new(10, 0);
}
