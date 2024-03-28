using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

public class UnitScreen : MonoBehaviour
{
    private SVGImage IconRenderer;
    private TMPro.TextMeshProUGUI CountText;

    public void Initialize(Sprite Sprite, bool bShowInverted)
    {
        CountText = transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        IconRenderer = transform.GetChild(1).GetComponent<SVGImage>();
        IconRenderer.sprite = Sprite;
        CountText.transform.localPosition = new Vector3(bShowInverted ? 15 : -15, 0, 0);
        IconRenderer.transform.localPosition = new Vector3(bShowInverted ? -8 : 8, 0, 0);
    }

    public void UpdateVisuals(int Count)
    {
        CountText.text = "" + Count;
    }
}
