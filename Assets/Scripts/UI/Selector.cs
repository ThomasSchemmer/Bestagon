using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Selector : MonoBehaviour{

    public void Awake() {
        CardSelector = new Selector<Card>();
        HexagonSelector = new Selector<HexagonVisualization>();
        CardSelector.Layer = "Card";
        HexagonSelector.Layer = "Hexagon";
    }

    public void Start() {
        Instance = this;
    }

    public void Update() {
        if (CardSelector.Raycast())
            return;

        HexagonSelector.Raycast();
    }

    public static Card GetSelectedCard() {
        if (!Instance)
            return null;

        return Instance.CardSelector.Selected;
    }

    public static HexagonVisualization GetSelectedHexagon() {
        if (!Instance)
            return null;

        return Instance.HexagonSelector.Selected;
    }

    public static void ForceDeselect() {
        Instance.CardSelector.Deselect(true);
        Instance.CardSelector.Deselect(false);
        Instance.HexagonSelector.Deselect(true);
        Instance.HexagonSelector.Deselect(false);
    }

    public Selector<Card> CardSelector;
    public Selector<HexagonVisualization> HexagonSelector;

    public static Selector Instance;
}

public class Selector<T> where T : Selectable
{
    public bool Raycast() {
        bool bIsLeftClick = Input.GetMouseButtonDown(0);
        bool bIsRightClick = Input.GetMouseButtonDown(1) && !bIsLeftClick;
        bool bIsEscapeClick = Input.GetKeyDown(KeyCode.Escape);

        if (bIsEscapeClick || !Raycast(out GameObject Hit, out bool bHasHitOtherUI)) { 
            Deselect(bIsLeftClick);
            return false;
        }

        // dont change selection/hovering, just swallow the specific interaction trigger
        if (bHasHitOtherUI)
            return true;

        T Target = Hit.GetComponent<T>();

        if (Target == null) {
            Deselect(bIsLeftClick);
            return false;
        }

        if (bIsRightClick) {
            Target.Interact();
            return true;
        }

        if ((bIsLeftClick && Target.IsEqual(Selected)) || (!bIsLeftClick && Target.IsEqual(Hovered))) {
            // we still hit something, even if its still the old selectable
            return true;
        }

        Deselect(bIsLeftClick);
        if (bIsLeftClick) {
            Selected = Target;
            Target.SetSelected(true);
            if (OnItemSelected != null) {
                OnItemSelected(Selected);
            }
        } else {
            Hovered = Target;
            Target.SetHovered(true);
            if (OnItemHovered != null) {
                OnItemHovered(Hovered);
            }
        }
        return true;
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

    private bool Raycast(out GameObject Hit, out bool bHasHitOtherUI) {
        if (RaycastUI(out Hit, out bHasHitOtherUI)) {
            return true;
        } 
            
        return RaycastWorld(out Hit);
    }

    private bool RaycastWorld(out GameObject Hit) {
        Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.forward * 10;

        // since we are using a orthogonal, angled camera we need to actually use its forward vector and cant use "Down"
        bool hasHit = Physics.Raycast(WorldPos, Camera.main.transform.forward, out RaycastHit RaycastHit, 1 << LayerMask.NameToLayer(Layer));

        Hit = hasHit ? RaycastHit.collider.gameObject : null;

        return hasHit;
    }

    private bool RaycastUI(out GameObject Hit, out bool bHasHitOtherUI) {
        bHasHitOtherUI = false;
        Hit = null;

        List<RaycastResult> Hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(GetPointerData(), Hits);

        if (Hits.Count == 0) 
            return false;

        foreach (RaycastResult Result in Hits) {
            if (Result.gameObject.layer == LayerMask.NameToLayer("UI")) {
                bHasHitOtherUI = true;
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
