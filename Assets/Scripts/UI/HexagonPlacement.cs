using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/** 
 * Service that generates selectable tiles to override the world generation
 */
public class HexagonPlacement : GameService
{
    protected override void StartServiceInternal()
    {
        gameObject.SetActive(true);
        Game.RunAfterServiceStart((MeshFactory Factory) =>
        {
            InitPlaceables();
        });
    }

    private void InitPlaceables()
    {
        HexagonConfig.HexagonType[] Types = (HexagonConfig.HexagonType[])Enum.GetValues(typeof(HexagonConfig.HexagonType));
        Types = Types[1..];
        for (int i = 0; i < Types.Length; i++)
        {
            GameObject Child = new GameObject();
            Child.transform.parent = this.transform;
            PlaceableHexagon Placeable = Child.AddComponent<PlaceableHexagon>();
            Placeable.Init(Types[i]);
        }
        Sort();
    }

    private void Sort()
    {
        int i = 0;
        int Count = transform.childCount;
        foreach (Transform Child in transform)
        {
            int Row = i / DISPLAY_PER_ROW;
            int Column = i % DISPLAY_PER_ROW;
            float Offset = Column - Count / 2.0f;
            Child.transform.localPosition = new Vector3(Offset * (PlaceableHexagon.SIZE + PlaceableHexagon.OFFSET), Row * PlaceableHexagon.SIZE, 0);
            i++;
        }
        Canvas.ForceUpdateCanvases();
    }

    protected override void StopServiceInternal()
    {
        gameObject.SetActive(false);
    }

    private static int DISPLAY_PER_ROW = 6;
}
