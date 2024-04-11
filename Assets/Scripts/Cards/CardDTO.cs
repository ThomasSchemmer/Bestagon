using Unity.Collections;
using UnityEditor;
using UnityEngine;

/** Helper class to transfer the cards between scenes. Only contains actually important data 
 * aka no visuals (as this is unnecessary to save and will be regenerated anyway) 
 * Could be replaced by buildingdata directly, but might have added stuff later
 */
public class CardDTO : ISaveable
{      
    public BuildingData BuildingData;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, BuildingData);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return BuildingData.GetStaticSize();
    }

    public void SetData(NativeArray<byte> Bytes)
    {        
        int Pos = 0;
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, BuildingData);
    }

    public CardDTO(Card Card)
    {
        Card.GetDTOData(out BuildingData);
    }

    public CardDTO(BuildingData Data)
    {
        BuildingData = Data;
    }

    public CardDTO() {
        BuildingData = ScriptableObject.CreateInstance<BuildingData>();
    }
}
