using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

public class SimpleIconScreen : MonoBehaviour
{
    protected SVGImage IconRenderer;

    public virtual void Initialize(Sprite Sprite, bool bShowRegular)
    {
        IconRenderer = transform.GetChild(0).GetComponent<SVGImage>();
        IconRenderer.sprite = Sprite;
        float x = IconRenderer.transform.localPosition.x;
        IconRenderer.transform.localPosition = new Vector3(bShowRegular ? x : -x, 0, 0);
    }

}
