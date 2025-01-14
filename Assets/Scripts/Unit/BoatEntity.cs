using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Boat", menuName = "ScriptableObjects/Boat", order = 4)]
[Serializable]
/**
 * Boat data, can be moved along the map but only on water
 * Can also transports scouts
 */
public class BoatEntity : TokenizedUnitEntity
{
    // only gets called once the scriptable object itself has been created!
    public BoatEntity()
    {
        RemainingMovement = MovementPerTurn;
    }

    public override void Init()
    {
        base.Init();
        SetName(GetPrefabName() + " " + ID);
        ID = MAX_BOAT_ID++;
        SetLocation(Location.Zero.ToSet());
    }

    public override string GetPrefabName()
    {
        return "Boat";
    }

    public override string GetName()
    {
        return Name.Replace(" ", "");
    }

    public void SetName(string NewName)
    {
        if (NewName.Length > MAX_NAME_LENGTH)
        {
            NewName = NewName[..MAX_NAME_LENGTH];
        }
        for (int i = NewName.Length; i < MAX_NAME_LENGTH; i++)
        {
            NewName = " " + NewName;
        }
        Name = NewName;
    }

    public override Production GetMovementRequirements()
    {
        return Production.Empty;
    }

    public override Vector3 GetOffset()
    {
        return new Vector3(0, 4.5f, 0);
    }

    public override Quaternion GetRotation()
    {
        return Quaternion.Euler(0, 60, 0);
    }

    protected override int GetFoodConsumption()
    {
        return 1;
    }

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (!base.IsInteractableWith(Hex, bIsPreview))
            return false;

        if (Hex.Data.HexHeight > HexagonConfig.HexagonHeight.Sea)
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Can only place in the ocean");
            }
            return false;
        }

        return true;
    }

    public override int GetTargetMeshIndex()
    {
        return 0;
    }

    public override Pathfinding.Parameters GetPathfindingParams()
    {
        return new(false, true, false);
    }

    protected override bool TryGetMovementAttribute(out AttributeType Type)
    {
        Type = default;
        return false;
    }

    [SaveableBaseType]
    [HideInInspector]
    public string Name;
    [HideInInspector]
    [SaveableBaseType]
    public int ID = 0;

    public static int MAX_NAME_LENGTH = 10;
    public static int MAX_BOAT_ID = 0;
}
