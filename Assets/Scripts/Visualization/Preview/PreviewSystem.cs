using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Manages all previews that can be created when hovering a selected card over a hexagon */
public class PreviewSystem : GameService
{
    public void Hide() {
        if (!IsInit)
            return;

        // do not delete yet, since moving over the hexes would retrigger constantly
        if (Preview)
        {
            Preview.gameObject.SetActive(false);
        }

        _OnPreviewHidden?.Invoke();
    }

    public void UpdatePreview()
    {
        if (!Game.TryGetServices(out Selectors Selector, out ReachVisualization ReachVisualization))
            return;

        Card SelectedCard = Selector.GetSelectedCard();
        HexagonVisualization HoveredHex = Selector.GetHoveredHexagon();
        if (!ShouldPreviewBeDisplayed())
        {
            Hide();
            ReachVisualization.Hide();
            ShowAdjacencyBonusFor(null, HoveredHex);
            return;
        }

        Show(SelectedCard, HoveredHex);
        if (SelectedCard is BuildingCard) {
            ReachVisualization.CheckFor(HoveredHex.Location);
        }
        ShowAdjacencyBonusFor(SelectedCard, HoveredHex);
    }

    private bool ShouldPreviewBeDisplayed()
    {
        if (!Game.TryGetServices(out Selectors Selector, out ReachVisualization ReachVisualization))
            return false;

        Card SelectedCard = Selector.GetSelectedCard();
        HexagonVisualization HoveredHex = Selector.GetHoveredHexagon();

        return SelectedCard && HoveredHex && HoveredHex.IsHovered() && SelectedCard.IsPreviewable();
    }

    private void ShowAdjacencyBonusFor(Card SelectedCard, HexagonVisualization SelectedHex)
    {
        if (!Game.TryGetService(out MapGenerator Generator))
            return;

        bool bIsVisible = SelectedCard ? SelectedCard.ShouldShowAdjacency(SelectedHex) : false;

        // check for each neighbour if it should be highlighted
        int Range = SelectedCard ? SelectedCard.GetAdjacencyRange() : MaxAdjacencyRange;
        Range = Mathf.Min(Range, MaxAdjacencyRange);
        List<HexagonVisualization> Neighbours = Generator.GetNeighbours(SelectedHex, true, Range);
        Dictionary<HexagonConfig.HexagonType, Production> Boni = new();
        if (SelectedCard && !SelectedCard.TryGetAdjacencyBonus(out Boni))
            return;

        foreach (HexagonVisualization Neighbour in Neighbours)
        {
            bool bIsAdjacent = false;
            if (bIsVisible)
            {
                if (Boni.TryGetValue(Neighbour.Data.Type, out _))
                {
                    bIsAdjacent = true;
                }
                if (SelectedCard && SelectedCard.IsCustomRuleApplying(Neighbour.Location))
                {
                    bIsAdjacent = false;
                }
            }
            Neighbour.SetAdjacent(bIsAdjacent);
            Neighbour.VisualizeSelection();
        }
    }

    public void Show(Card Card, HexagonVisualization Hex) {
        if (!IsInit)
            return;

        SetPreview(Card);

        Preview.gameObject.SetActive(true);
        Preview.Show(Hex);
        PreviewLocation = Hex.Location;

        _OnPreviewShown?.Invoke();
    }

    private void SetPreview(Card Card)
    {
        if (Preview != null && Preview.IsFor(Card))
            return;

        if (Preview != null)
        {
            DestroyImmediate(Preview.gameObject);
        }

        Preview = CardPreview.CreateFor(Card);
    }

    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke(this);
    }

    public bool HasPreviewableAs<T>() where T : IPreviewable
    {
        if (Preview == null)
            return false;

        IPreviewable Previewable = Preview.GetPreviewable();
        if (Previewable == null)
            return false;

        return Previewable is T;
    }

    public T GetPreviewableAs<T>() where T : IPreviewable
    {
        if (Preview == null)
            return default;

        return Preview.GetPreviewableAs<T>();
    }

    public Location GetPreviewLocation()
    {
        return PreviewLocation;
    }

    protected override void StopServiceInternal() {}

    public Material PreviewMaterial;
    public GameObject UIContainer;

    private CardPreview Preview;
    private Location PreviewLocation;

    public delegate void OnPreviewShown();
    public delegate void OnPreviewHidden();
    public OnPreviewShown _OnPreviewShown;
    public OnPreviewHidden _OnPreviewHidden;

    public static int MaxAdjacencyRange = 2;
}
