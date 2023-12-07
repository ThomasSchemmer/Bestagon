﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stockpile : MonoBehaviour
{
    public void Start() {
        Instance = this;

        Tuple<Production.Type, int>[] Tuples = {
            new(Production.Type.Wood, 10),
            new(Production.Type.Stone, 10),
            new(Production.Type.Metal, 10),
            new(Production.Type.Food, 10)
        };
        Resources = new Production(Tuples);
    }

    public void Update() {
        ShowResources();
    }

    private void ShowResources() {
        string ResourceString = Resources.GetDescription() + "\t\tTurn: " + Turn.Instance.TurnNr + "\t\tWorking: "+Workers.GetAssignedWorkerCount() + "\tIdle: " + Workers.GetUnassignedWorkerCount();
        ResourceText.SetText(ResourceString);
    }

    public static bool Pay(Production Costs) {
        if (!CanAfford(Costs))
            return false;

        Instance.Resources -= Costs;
        Instance.ShowResources();
        return true;
    }

    public static void ProduceResources() {
        if (!Instance)
            return;

        Instance._ProduceResources();
    }

    public void _ProduceResources() {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        Resources += MapGenerator.GetProductionPerTurn();
        Resources -= Workers.GetWorkerCosts();
        if (Resources[Production.Type.Food] < 0) {
            Workers.Starve(Resources[Production.Type.Food]);
            Resources[Production.Type.Food] = 0;
        }

        ShowResources();
    }

    private static bool CanAfford(Production Costs) {
        return Costs <= Instance.Resources;
    }

    public TextMeshProUGUI ResourceText;

    public Production Resources;

    public static Stockpile Instance;
}
