using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stockpile : MonoBehaviour
{
    public void Start() {
        Instance = this;
    }

    public void Update() {
        ShowResources();
    }

    private void ShowResources() {
        string ResourceString = Resources.GetDescription() + "\t\tTurn: " + Turn.TurnNr + "\t\tWorking: "+Workers.GetAssignedWorkerCount() + "\tIdle: " + Workers.GetUnassignedWorkerCount();
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
        Resources += MapGenerator.GetProductionPerTurn();
        Resources -= Workers.GetWorkerCosts();
        if (Resources.Food < 0) {
            Workers.Starve(Resources.Food);
            Resources.Food = 0;
        }

        ShowResources();
    }

    private static bool CanAfford(Production Costs) {
        return Costs <= Instance.Resources;
    }

    public TextMeshProUGUI ResourceText;

    public Production Resources = new Production(10, 10, 10, 10);

    public static Stockpile Instance;
}
