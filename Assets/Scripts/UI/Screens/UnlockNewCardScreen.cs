using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Screen showing a single card that just has been unlocked
 */
public class UnlockNewCardScreen : CollectChoiceScreen
{
    public CardCollection Target;

    private void Start()
    {
        Container = transform.GetChild(0).gameObject;
        Hide();
    }

    public override void OnSelectOption(int ChoiceIndex)
    {
        base.OnSelectOption(ChoiceIndex);
        Hide();
    }

    public void OnClickUnlock()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        if (!Stockpile.CanAffordUpgrade(GetUpgradeCostsForChoice(0)))
            return;

        Show();
        Create();
    }

    protected override CardDTO.Type GetCardTypeAt(int i)
    {
        return CardDTO.Type.Building;
    }

    protected override bool ShouldCardBeUnlocked(int i)
    {
        return true;
    }

    protected override Production GetCostsForChoice(int i)
    {
        return Production.Empty;
    }
    protected override int GetUpgradeCostsForChoice(int i)
    {
        return 5;
    }
    protected override CardCollection GetTargetCardCollection()
    {
        return Target;
    }

    protected override int GetSeed()
    {
        if (!Game.TryGetService(out BuildingService BuildingService))
            return 0;

        return BuildingService.UnlockableBuildings.GetCountOfState(Unlockables.State.Unlocked);
    }
    protected override int GetWorkerCostsForChoice(int i)
    {
        return 0;
    }
}
