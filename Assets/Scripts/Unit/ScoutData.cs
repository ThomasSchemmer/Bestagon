using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Scout", menuName = "ScriptableObjects/Scout", order = 4)]
[Serializable]
public class ScoutData : TokenizedUnitData
{   
    // only gets called once the scriptable object itself has been created!
    public ScoutData() {
        RemainingMovement = MovementPerTurn;
    }

    public override void Init()
    {
        base.Init();
        SetName(GetPrefabName() + " " + ID);
        ID = MAX_SCOUT_ID++;
    }

    public override string GetPrefabName()
    {
        return "Scout";
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return TokenizedUnitData.GetStaticSize() + MAX_NAME_LENGTH * sizeof(int) + sizeof(int);
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddString(Bytes, Pos, Name);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ID);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetString(Bytes, Pos, MAX_NAME_LENGTH, out Name);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ID);
    }

    public string GetName()
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

    public override bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (Hex.Data.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
        {
            if (!bIsPreview)
            {
                MessageSystem.CreateMessage(Message.Type.Error, "Can only place on scouted tiles");
            }
            return false;
        }

        if (Hex.Data.HexHeight < HexagonConfig.HexagonHeight.Flat || Hex.Data.HexHeight > HexagonConfig.HexagonHeight.Hill)
        {
            if (!bIsPreview)
            {
                MessageSystem.CreateMessage(Message.Type.Error, "Cannot place on Mountains or in the ocean");
            }
            return false;
        }

        return true;
    }

    [HideInInspector]
    public string Name;
    [HideInInspector]
    public int ID = 0;

    public static int MAX_NAME_LENGTH = 10;
    public static int MAX_SCOUT_ID = 0;
}
