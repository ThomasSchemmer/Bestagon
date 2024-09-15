using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Entity representing decoration data on some hexagon, managed by @DecorationService
 * and visualized through chunk
 */
[CreateAssetMenu(fileName = "Decoration", menuName = "ScriptableObjects/Decoration", order = 4)]
[Serializable]
public class DecorationEntity : ScriptableEntity, ITokenized, IPreviewable
{
    public enum DType {
        Ruins = 1,
        Tribe = 2,
        Treasure = 3,
    }

    public DType DecorationType;
    public DecorationVisualization Visualization;

    private Location Location;

    public DecorationEntity() {
        EntityType = EType.Decoration;
    }

    public void SetLocation(Location Location)
    {
        this.Location = Location;
    }

    public Location GetLocation()
    {
        return this.Location;
    }

    public void SetVisualization(EntityVisualization Vis)
    {
        if (Vis is not DecorationVisualization)
            return;

        Visualization = Vis as DecorationVisualization;
    }

    public string GetDecorationText()
    {
        switch (DecorationType)
        {
            case DType.Ruins: return "Contains ancient ruins";
            case DType.Tribe: return "Contains unknown tribe";
            case DType.Treasure: return "Contains treasure chest";
            default: return "";
        }
    }

    public override int GetSize()
    {
        throw new NotImplementedException();
    }

    public override byte[] GetData()
    {
        throw new NotImplementedException();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        throw new NotImplementedException();
    }

    public virtual Vector3 GetOffset()
    {
        return new Vector3(0, 5, 0);
    }

    public virtual Quaternion GetRotation()
    {
        return Quaternion.Euler(0, 180, 0);
    }

    public bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        throw new NotImplementedException();
    }
}
