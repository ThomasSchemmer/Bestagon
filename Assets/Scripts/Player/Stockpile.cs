using Codice.Client.BaseCommands;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stockpile : GameService
{

    public void Update() {
        ShowResources();
    }

    private void ShowResources() {
        string ResourcesString = Resources.GetDescription() + "\t\t";
        Turn Turn = Game.GetService<Turn>();
        string TurnString = "Turn: " + (Turn ? Turn.TurnNr : -1) + "\t\t";

        string WorkersString = "";
        if (Game.TryGetService(out Workers WorkerService))
        {
            WorkersString = "Working: " + WorkerService.GetEmployedWorkerCount() + "\tIdle: " + WorkerService.GetUnemployedWorkerCount();
        }
        string FinalString = ResourcesString + TurnString + WorkersString;
        ResourceText.SetText(FinalString);
    }

    public bool Pay(Production Costs) {
        if (!CanAfford(Costs))
            return false;

        Resources -= Costs;
        ShowResources();
        return true;
    }

    public void ProduceWorkers()
    {
        if (!Game.TryGetServices(out MapGenerator MapGenerator, out Workers WorkerService))
            return;

        if (!MapGenerator.TryGetAllBuildings(out List<BuildingData> Buildings))
            return;

        foreach (BuildingData Building in Buildings)
        {
            if (Building.Effect.EffectType != OnTurnBuildingEffect.Type.YieldWorkerPerWorker)
                continue;

            if (Building.WorkerCount != 2)
                continue;

            Building.RemoveWorker();
            Building.RemoveWorker();
            WorkerService.CreateNewWorker();
        }
    }

    public void ProduceResources() {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!Game.TryGetServices(out Workers WorkerService, out Units UnitService))
            return;

        Resources += MapGenerator.GetProductionPerTurn();
        Resources -= WorkerService.GetWorkerCosts();
        Resources -= UnitService.GetUnitCosts();
        HandleStarvation(WorkerService, UnitService);

        ShowResources();
    }

    private void HandleStarvation(Workers WorkerService, Units UnitService)
    {
        if (Resources[Production.Type.Food] >= 0)
            return;

        int WorkersKilled = WorkerService.HandleStarvation(Resources[Production.Type.Food]);
        Resources[Production.Type.Food] += WorkersKilled * Workers.CostsPerWorker[Production.Type.Food]; 
        
        if (Resources[Production.Type.Food] >= 0)
            return;

        int UnitsKilled = UnitService.HandleStarvation(Resources[Production.Type.Food]);

        Resources[Production.Type.Food] = 0;
    }

    public bool CanAfford(Production Costs) {
        return Costs <= Resources;
    }

    protected override void StartServiceInternal()
    {
        Tuple<Production.Type, int>[] Tuples = {
            new(Production.Type.Wood, 4),
            new(Production.Type.Stone, 2),
            new(Production.Type.Metal, 0),
            new(Production.Type.Food, 5)
        };
        Resources = new Production(Tuples);
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal()
    {
        throw new System.NotImplementedException();
    }

    public TextMeshProUGUI ResourceText;

    public Production Resources;
}
