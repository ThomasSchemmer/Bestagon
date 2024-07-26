using System;
using UnityEngine;

/** 
 * Service that generates selectable tiles to override the world generation
 * Used in the map editor
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
        for (int i = 0; i < Types.Length; i++)
        {
            GameObject Child = new GameObject();
            Child.transform.SetParent(transform, false);
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
            float Offset = Column - DISPLAY_PER_ROW / 2.0f;

            float PosX = Offset * (PlaceableHexagon.SIZE + PlaceableHexagon.OFFSET);
            float PosY = Row * PlaceableHexagon.SIZE;
            RectTransform ChildRect = Child.GetComponent<RectTransform>();
            ChildRect.anchoredPosition = new Vector3(PosX, PosY, 0);
            i++;
        }
        Canvas.ForceUpdateCanvases();
    }

    protected override void StopServiceInternal()
    {
        gameObject.SetActive(false);
    }

    private static int DISPLAY_PER_ROW = 9;
}
