using Unity.Collections;
using UnityEditor;
using UnityEngine;

/** Helper class to transfer the cards between scenes */
public class CardDTO : ISaveable
{      
    public GUID ID;
    public BuildingData BuildingData;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddString(Bytes, Pos, ID.ToString());
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, BuildingData);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return BuildingData.GetStaticSize() + 32;
    }

    public void SetData(NativeArray<byte> Bytes)
    {        
        int Pos = 0;
        Pos = SaveGameManager.GetString(Bytes, Pos, 32, out string GUIDString);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, BuildingData);
        
        GUID.TryParse(GUIDString, out ID);
    }

    public CardDTO(Card Card)
    {
        Card.GetDTOData(out this.ID, out this.BuildingData);
    }

    public CardDTO() {
        BuildingData = ScriptableObject.CreateInstance<BuildingData>();
    }
}
