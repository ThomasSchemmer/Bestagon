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
    public int DisplayCount = 5;
    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceStart((BuildingFactory Factory) =>
        {
            InitPlaceables();
        });
    }

    private void InitPlaceables()
    {
        HexagonConfig.HexagonType[] Types = (HexagonConfig.HexagonType[])Enum.GetValues(typeof(HexagonConfig.HexagonType));
        Types = Types[1..];
        int Max = Mathf.Min(DisplayCount, Types.Length);
        for (int i = 0; i < Max; i++)
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
            float offset = i - Count / 2.0f;
            Child.transform.localPosition = new Vector3(offset * (PlaceableHexagon.SIZE + PlaceableHexagon.OFFSET), 0, 0);
            i++;
        }
        Canvas.ForceUpdateCanvases();
    }

    protected override void StopServiceInternal() {}
}
