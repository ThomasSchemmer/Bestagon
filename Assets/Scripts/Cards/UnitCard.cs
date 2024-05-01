
public class UnitCard : Card
{
    public TokenizedUnitData UnitData;

    public void Init(UnitData UnitData, int Index)
    {
        this.UnitData = (TokenizedUnitData)UnitData;
        base.Init(Index);
        
    }

    public override string GetName()
    {
        return UnitData.Type.ToString();
    }

    protected override CardCollection GetUseTarget()
    {
        if (!Game.TryGetService(out DiscardDeck Discard))
            return null;

        return Discard;
    }

    protected override void UseInternal() {}
}
