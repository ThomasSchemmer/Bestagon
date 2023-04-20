using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Selectable {

    void SetSelected(bool Selected);
    void SetHovered(bool Hovered);

    void Interact();

    bool IsEqual(Selectable other);
}
