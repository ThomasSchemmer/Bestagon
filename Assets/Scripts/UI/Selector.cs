using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * Helper class to wrap all templated selectors into one easily accesible UI element / gameservice
 */
public class Selector : GameService{
    protected override void StartServiceInternal()
    {
        CardSelector = new Selector<Card>();
        HexagonSelector = new Selector<HexagonVisualization>();
        UISelector = new Selector<UIElement>(true);
        CardSelector.Layer = "Card";
        HexagonSelector.Layer = "Hexagon";
        UISelector.Layer = "UI";
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }

    protected override void StopServiceInternal() {}

    public void Update() {
        if (!IsEnabled)
            return;

        if (UISelector.RayCast())
            return;

        if (CardSelector.RayCast())
            return;

        HexagonSelector.RayCast();
    }

    private void OnPause()
    {
        IsEnabled = false;
        ForceDeselect();
    }

    private void OnResume()
    {
        IsEnabled = true;
    }

    public Card GetSelectedCard() {
        return CardSelector.Selected;
    }

    public HexagonVisualization GetSelectedHexagon() {
        return HexagonSelector.Selected;
    }

    public UIElement GetSelectedUIElement() 
    {
        return UISelector.Selected;
    }

    public void SelectHexagon(HexagonVisualization Vis) {
        HexagonSelector.Select(Vis);
    }

    public void DeselectHexagon() { 
        HexagonSelector.Deselect(true);
    }

    public void ForceDeselect() {
        CardSelector.Deselect(true);
        CardSelector.Deselect(false);
        HexagonSelector.Deselect(true);
        HexagonSelector.Deselect(false);
        UISelector.Deselect(true);
        UISelector.Deselect(false);
    }

    public Selector<Card> CardSelector;
    public Selector<HexagonVisualization> HexagonSelector;
    public Selector<UIElement> UISelector;

    private bool IsEnabled = true;
}

/**
 * Class that automates selecting gameobjects with the mouse easy. Checks by type if a hovered object is selectable
 * and calls the Selectable interface accordingly
 */
public class Selector<T> where T : Selectable
{
    public Selector(bool bIsUIOnly = false) {
        this.bIsUIOnly = bIsUIOnly;
    }

    public bool RayCast() {
        bool bIsLeftClick = Input.GetMouseButtonDown(0);
        bool bIsRightClick = Input.GetMouseButtonDown(1) && !bIsLeftClick;
        bool bIsEscapeClick = Input.GetKeyDown(KeyCode.Escape);

        if (bIsEscapeClick || !RayCast(out GameObject Hit)) { 
            Deselect(bIsLeftClick);
            return false;
        }

        if (!Hit) {
            // can only be true if Unity UI has been hit (eg a button). Simply swallow the input
            return true;
        }

        T Target = Hit.GetComponent<T>();

        if (Target == null) {
            Deselect(bIsLeftClick);
            return false;
        }

        if (bIsRightClick) {
            Target.Interact();
            return true;
        }

        if (bIsLeftClick) {
            // we do intentionally not return, since this event should trigger on every click
            Target.ClickOn(GetPointerData().position);
        }

        if ((bIsLeftClick && Target.IsEqual(Selected)) || (!bIsLeftClick && Target.IsEqual(Hovered))) {
            // we still hit something, even if its still the old selectable
            return true;
        }

        Deselect(bIsLeftClick);
        if (bIsLeftClick) {
            Select(Target);
        } else {
            Hover(Target);
        }
        return true;
    }

    public void Select(T Target) {
        Selected = Target;
        Target.SetSelected(true);
        if (OnItemSelected != null) {
            OnItemSelected(Selected);
        }
    }

    public void Hover(T Target) {
        Hovered = Target;
        Target.SetHovered(true);
        if (OnItemHovered != null) {
            OnItemHovered(Hovered);
        }
    }

    public void Deselect(bool bIsClicked) {
        if (bIsClicked) {
            if (Selected != null) {
                Selected.SetSelected(false);
                Selected = default(T);
                if (OnItemDeSelected != null) {
                    OnItemDeSelected();
                }
            }
        } else {
            if (Hovered != null) {
                Hovered.SetHovered(false);
                Hovered = default(T);
                if (OnItemDeHovered != null) {
                    OnItemDeHovered();
                }
            }
        }
    }

    private bool RayCast(out GameObject Hit) {
        if (RayCastUI(out Hit)) {
            return true;
        }
           
        return !bIsUIOnly && RayCastWorld(out Hit);
    }

    private bool RayCastWorld(out GameObject Hit) {
        Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.forward * 10;

        // since we are using a orthogonal, angled camera we need to actually use its forward vector and cant use "Down"
        bool hasHit = Physics.Raycast(WorldPos, Camera.main.transform.forward, out RaycastHit RaycastHit, 1 << LayerMask.NameToLayer(Layer));

        Hit = hasHit ? RaycastHit.collider.gameObject : null;

        return hasHit;
    }

    private bool RayCastUI(out GameObject Hit) {
        Hit = null;

        List<RaycastResult> Hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(GetPointerData(), Hits);

        if (Hits.Count == 0) 
            return false;

        foreach (RaycastResult Result in Hits) {
            if (!bIsUIOnly && Result.gameObject.layer == LayerMask.NameToLayer("UI")) {
                return true;
            }
            if (Result.gameObject.layer == LayerMask.NameToLayer(Layer)) {
                Hit = Result.gameObject;
                return true;
            }
        }

        return false;
    }

    static PointerEventData GetPointerData() {
        return new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };
    }

    public bool bIsUIOnly = false;
    public T Selected;
    public T Hovered;

    public string Layer;

    public delegate void _ItemInteracted(T Item);
    public delegate void _ItemNotInteracted();
    public event _ItemInteracted OnItemSelected;
    public event _ItemNotInteracted OnItemDeSelected;
    public event _ItemInteracted OnItemHovered;
    public event _ItemNotInteracted OnItemDeHovered;
}
