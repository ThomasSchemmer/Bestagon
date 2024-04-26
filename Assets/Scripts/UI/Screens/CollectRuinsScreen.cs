using UnityEngine;

public class CollectRuinsScreen : MonoBehaviour
{
    public GameObject CardContainer;

    private BuildingConfig.Type BuildingToUnlock;
    private Card GeneratedCard;
    private GameObject Container;
    private Location CurrentLocation;

    private void Start()
    {
        HexagonVisualization._OnMovementTo += HandleMovement;
        Container = transform.GetChild(0).gameObject;
        Hide();
    }

    private void OnDestroy()
    {
        HexagonVisualization._OnMovementTo -= HandleMovement;
    }

    public void OnSelectOptionA()
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardHand CardHand))
            return;

        Unlockables.UnlockSpecificBuildingType(BuildingToUnlock);
        CardHand.AddCard(GeneratedCard);
        RemoveRuins();
        Hide();
    }

    public void OnSelectOptionB()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.UpgradePoints += 2;

        Destroy(GeneratedCard.gameObject);
        RemoveRuins();
        Hide();
    }

    private void RemoveRuins()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out HexagonData HexData))
            return;

        HexData.Decoration = HexagonConfig.HexagonDecoration.None;
        if (!MapGenerator.TryGetHexagon(CurrentLocation, out HexagonVisualization HexVis))
            return;

        HexVis.UpdateMesh();
    }

    public void Show()
    {
        Game.Instance.OnOpenMenu();
        Container.SetActive(true);
        GenerateAndShowCard();
    }

    private void HandleMovement(Location Location)
    {
        CurrentLocation = Location;
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagonData(Location, out HexagonData HexData))
            return;

        if (HexData.Decoration != HexagonConfig.HexagonDecoration.Ruins)
            return;

        Show();
    }

    private void GenerateAndShowCard()
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardFactory CardFactory))
            return;

        if (!Unlockables.TryUnlockNewBuildingType(out BuildingToUnlock, true))
            return;

        CardFactory.CreateCard(BuildingToUnlock, 0, CardContainer.transform, SetGeneratedCard);
    }

    private void SetGeneratedCard(Card Card)
    {
        GeneratedCard = Card;
    }

    public void Hide()
    {
        Game.Instance.OnCloseMenu();
        Container.SetActive(false);
    }
}
