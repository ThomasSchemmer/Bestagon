using UnityEngine;

/** Interface to unify access to display preview meshes */
public interface IPreviewable 
{
    public Vector3 GetOffset();
    public Quaternion GetRotation();
    public bool CanBeInteractedOn(HexagonVisualization Hex);
}
