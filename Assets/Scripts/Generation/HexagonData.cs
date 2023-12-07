using System.Diagnostics;
using Unity.Mathematics;
using static HexagonConfig;

public class HexagonData 
{
    public Location Location;
    public HexagonType Type;
    public float Value;
    public float Height;
    public bool bIsMalaised;

    /** 
     * Converts the data into a transferable, lightweight object. 
     * Only contains data necessary for the minimap
     */
    public HexagonDTO GetDTO() {

        uint uType = (uint)MaskToInt((int)Type, 16) + 1;
        uint Malaise = (uint)(bIsMalaised ? 1 : 0) << 7;

        return new HexagonDTO() {
            Type = uType + Malaise,
        };
    }
}
