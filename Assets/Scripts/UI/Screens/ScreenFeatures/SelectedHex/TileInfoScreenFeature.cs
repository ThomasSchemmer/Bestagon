

using TMPro;

public class TileInfoScreenFeature : ScreenFeature<HexagonData>
{
    private TextMeshProUGUI TileText, LocationText;

    public override void Init(ScreenFeatureGroup<HexagonData> Target)
    {
        base.Init(Target);
        TileText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        LocationText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }

    public override bool ShouldBeDisplayed()
    {
        return Target.GetFeatureObject() != null;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        HexagonData SelectedHex = Target.GetFeatureObject();
        TileText.text =
            SelectedHex.Type.ToString() + "\n" +
            SelectedHex.HexHeight;
        LocationText.text = SelectedHex.Location.GlobalTileLocation.ToString();
    }

    public override void Hide()
    {
        base.Hide();
        TileText.text = string.Empty;
        LocationText.text = string.Empty;
    }
}
