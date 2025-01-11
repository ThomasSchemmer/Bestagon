using UnityEngine;

/** Interface to unify access to display preview meshes */
public interface IPreviewable 
{
    /** Returns the offset required by the object to be previewed correctly */
    public Vector3 GetOffset();

    /** Returns the rotation required by the object to be previewed correctly */
    public Quaternion GetRotation();

    /** Checks if the object is interactable with the passed in hex, will create warnings if not preview */
    public bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview);
}
