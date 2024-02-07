using Unity.Collections;
using UnityEditor;
using UnityEngine;

/** Helper class to transfer the cards between scenes */
public class CardDTO : ISaveable
{      
    public GUID ID;
    public BuildingData BuildingData;
    public bool bIsActive = false;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddString(Bytes, Pos, ID.ToString());
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, BuildingData);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsActive);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return BuildingData.GetStaticSize() + 32 + sizeof(byte);
    }

    public void SetData(NativeArray<byte> Bytes)
    {        
        int Pos = 0;
        Pos = SaveGameManager.GetString(Bytes, Pos, 32, out string GUIDString);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, BuildingData);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bIsActive);
        
        GUID.TryParse(GUIDString, out ID);
    }

    public CardDTO(Card Card)
    {
        Card.GetDTOData(out this.ID, out this.BuildingData);
        this.BuildingData.bIncludeWorker = false;
    }

    public CardDTO() {
        BuildingData = ScriptableObject.CreateInstance<BuildingData>();
    }
}
