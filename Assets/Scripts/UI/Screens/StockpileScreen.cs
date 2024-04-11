using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockpileScreen : MonoBehaviour
{
    public void Start()
    {
        Game.RunAfterServiceInit((Stockpile Stockpile, Turn Turn) =>
        {
            Game.RunAfterServiceInit((IconFactory IconFactory) =>
            {
                Stockpile._OnResourcesChanged += UpdateVisuals;
                Turn._OnTurnEnd += UpdateIndicatorCount;
                Initialize(Stockpile, IconFactory);
            });
        });

    }

    private void Initialize(Stockpile Stockpile, IconFactory IconFactory)
    {
        int GroupCount = Production.Indices.Length - 1;
        GroupScreens = new StockpileGroupScreen[GroupCount];
        for (int i = 0; i < GroupCount; i++)
        {
            GameObject GroupObject = Instantiate(GroupPrefab);
            StockpileGroupScreen Screen = GroupObject.GetComponent<StockpileGroupScreen>();
            Screen.Initialize(i, ItemPrefab, Stockpile, IconFactory);
            Screen.transform.SetParent(transform, false);
            Screen.transform.position = new Vector3(
                (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * i,
                0,
                0)
                + Screen.transform.position;
            GroupScreens[i] = Screen;
        }
    }

    private void UpdateIndicatorCount()
    {
        foreach (StockpileGroupScreen GroupScreen in GroupScreens) {
            GroupScreen.UpdateIndicatorCount();
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        foreach (StockpileGroupScreen Group in GroupScreens)
        {
            Group.UpdateVisuals();
        }
    }

    public GameObject GroupPrefab;
    public GameObject ItemPrefab;

    private StockpileGroupScreen[] GroupScreens;
}
