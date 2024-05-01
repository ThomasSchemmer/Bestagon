
public class UnitCard : Card
{
    public TokenizedUnitData UnitData;

    public override string GetName()
    {
        return "UnitCard";
    }

    protected override CardCollection GetUseTarget()
    {
        if (!Game.TryGetService(out DiscardDeck Discard))
            return null;

        return Discard;
    }

    protected override void UseInternal() {}
}
