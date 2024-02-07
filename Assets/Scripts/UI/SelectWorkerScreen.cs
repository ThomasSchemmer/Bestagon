using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectWorkerScreen : MonoBehaviour
{
    public void Show(bool bShow) {
        this.gameObject.SetActive(bShow);
        if (!bShow)
            return;

        FillWorkerContent(true);
        FillWorkerContent(false);
    }

    public void OpenForBuilding(BuildingData Building) {
        this.Building = Building;
        InitWorkerDistances();
        Show(true);
    }

    private void FillWorkerContent(bool bFillAssigned) {
        Transform Container = bFillAssigned ? AssignedWorkerContent.transform : UnassignedWorkerContent.transform;
        PriorityQueue<WorkerData> SelectedWorker = bFillAssigned ? AssignedWorker : UnassignedWorker;
        int ShowIndex = bFillAssigned ? AssignedWorkerShowIndex : UnassignedWorkerShowIndex;

        foreach (Transform Transform in Container) {
            Destroy(Transform.gameObject);
        }

        int MaxUnassignedShown = Mathf.Min(MaxWorkerShown, SelectedWorker.Count - ShowIndex);
        for (int i = 0; i < MaxUnassignedShown; i++) {
            // use tuple data to create ui element
            Tuple<WorkerData, int> WorkerTuple = SelectedWorker[ShowIndex + i];
            WorkerData Worker = WorkerTuple.Key;
            int MovementCost = WorkerTuple.Value;

            int Turns = (int)Mathf.Ceil((float)MovementCost / WorkerTuple.Key.MovementPerTurn);

            GameObject WorkerObject = Instantiate(SelectWorkerPrefab, Container);
            WorkerObject.transform.localPosition = ContainerWorkerOffset + WorkerOffset * i;
            WorkerObject.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = Worker.GetName();
            WorkerObject.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = MovementCost + " / " + Turns;
            WorkerObject.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => {
                AssignWorkerToBuilding(Worker);
            });
        }
    }

    private void AssignWorkerToBuilding(WorkerData Worker)
    {
        if (!Game.TryGetService(out Workers WorkerService))
            return;

        if (Worker.AssignedBuilding != null) {
            Worker.RemoveFromBuilding();
            WorkerService.ReturnWorker(Worker);
        }

        Building.AddWorker(Worker);
        WorkerService.MakeWorkerWork(Worker);
        Show(false);
        SelectedHex.Show();
        InitWorkerDistances();
    }

    public void InitWorkerDistances() {
        UnassignedWorker = new();
        AssignedWorker = new();
        AssignedWorkerShowIndex = 0;
        UnassignedWorkerShowIndex = 0;

        if (!Game.TryGetService(out Workers WorkerService))
            return;

        foreach (WorkerData Worker in WorkerService.UnassignedWorker) {
            if (Worker.AssignedBuilding != null && Worker.AssignedBuilding.Equals(Building))
                continue;

            int MovementCost = Worker.GetMovementCostTo(Building.Location);
            if (MovementCost < 0)
                continue;

            UnassignedWorker.Enqueue(Worker, MovementCost);
        }

        foreach (WorkerData Worker in WorkerService.AssignedWorker) {
            if (Worker.AssignedBuilding != null && Worker.AssignedBuilding.Equals(Building))
                continue;

            int MovementCost = Worker.GetMovementCostTo(Building.Location);
            if (MovementCost < 0)
                continue;

            AssignedWorker.Enqueue(Worker, MovementCost);
        }
    }

    private int GetTurnsFromMovement(WorkerData Worker, int MovementCosts) {
        return (int)Mathf.Ceil((float)MovementCosts / Worker.MovementPerTurn);
    }

    public BuildingData Building;
    public GameObject AssignedWorkerContent, UnassignedWorkerContent;
    public GameObject SelectWorkerPrefab;
    public Button CloseButton;

    public SelectedHex SelectedHex;

    public PriorityQueue<WorkerData> UnassignedWorker;
    public PriorityQueue<WorkerData> AssignedWorker;

    private int AssignedWorkerShowIndex = 0;
    private int UnassignedWorkerShowIndex = 0;

    public static Vector3 WorkerOffset = new Vector3(125, 0, 0);
    public static Vector3 ContainerWorkerOffset = new Vector3(-275, 30, 0);
    public static int MaxWorkerShown = 4;
}
