using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Scout", menuName = "ScriptableObjects/Scout", order = 4)]
[Serializable]
/** 
 * Scout data, can be moved through the map to reveal new tiles and collect decorations
 */
public class ScoutEntity : TokenizedUnitEntity
{   
    // only gets called once the scriptable object itself has been created!
    public ScoutEntity() {
        RemainingMovement = MovementPerTurn;
    }

    public override void Init()
    {
        base.Init();
        SetName(GetPrefabName() + " " + ID);
        ID = MAX_SCOUT_ID++;
        SetLocation(Location.Zero.ToSet());
    }

    public override string GetPrefabName()
    {
        return "Scout";
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
        Production Requirements = Production.Empty;
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return Requirements;

        if (!MapGenerator.TryGetHexagonData(Location, out HexagonData HexData))
            return Requirements;

        switch (HexData.Type)
        {
            case HexagonConfig.HexagonType.Desert:
            case HexagonConfig.HexagonType.Savanna:
                Requirements += new Production(Production.Type.WaterSkins, 1);
                break;
        }

        return Requirements;
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
        AttributeSet Attributes = AttributeSet.Get();
        return (int)Attributes[AttributeType.ScoutFoodConsumption].CurrentValue;
    }

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (!base.IsInteractableWith(Hex, bIsPreview))
            return false;

        if (Hex.Data.HexHeight < HexagonConfig.HexagonHeight.Flat || Hex.Data.HexHeight > HexagonConfig.HexagonHeight.Hill)
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot place on Mountains or in the ocean");
            }
            return false;
        }

        return true;
    }

    public override int GetTargetMeshIndex()
    {
        return HasDog() ? 1 : 0;
    }

    public bool HasDog()
    {
        return AttributeSet.Get()[AttributeType.ScoutDogAmount].CurrentValue > 0;
    }

    public override Pathfinding.Parameters GetPathfindingParams()
    {
        return Pathfinding.Parameters.Standard;
    }

    [SaveableBaseType]
    [HideInInspector]
    public string Name;
    [HideInInspector]
    [SaveableBaseType]
    public int ID = 0;

    public static int MAX_NAME_LENGTH = 10;
    public static int MAX_SCOUT_ID = 0;
}
