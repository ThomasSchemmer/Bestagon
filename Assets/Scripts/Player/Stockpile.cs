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
        string ResourceString = Resources.GetDescription() + "\t\tTurn: " + Turn.TurnNr + "\t\tWorkers: "+Workers.GetNrOfWorkingWorker() + "\tIdle: " + Workers.GetNrOfIdleWorker();
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
        Production ProducedResources = MapGenerator.GetProductionPerTurn();
        Resources += ProducedResources;
        ShowResources();
    }

    private static bool CanAfford(Production Costs) {
        return Costs <= Instance.Resources;
    }

    public TextMeshProUGUI ResourceText;

    public Production Resources = new Production(10, 10, 10, 10);

    public static Stockpile Instance;
}
