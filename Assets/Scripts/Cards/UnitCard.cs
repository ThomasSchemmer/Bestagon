
public class UnitCard : Card
{
    public TokenizedUnitData Unit;

    public void Init(UnitData UnitData, int Index)
    {
        this.Unit = (TokenizedUnitData)UnitData;
        base.Init(Index);
    }

    public override string GetName()
    {
        return Unit.Type.ToString();
    }

    protected override CardCollection GetUseTarget()
    {
        if (!Game.TryGetService(out DiscardDeck Discard))
            return null;

        return Discard;
    }

    protected override void UseInternal() {}

    public override bool IsPreviewable()
    {
        return Unit.Type == UnitData.UnitType.Scout;
    }

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return true;
    }

    public override void InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Selector Selector, out Units Units))
            return;

        if (Units.IsUnitAt(Hex.Location))
        {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create unit here - one already exists");
            return;
        }

        Units.AddUnit(Unit);
        Unit.MoveTo(Hex.Location, 0);

        Selector.ForceDeselect();
        Selector.SelectHexagon(Hex);

        Use();
    }
}
