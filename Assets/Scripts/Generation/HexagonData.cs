using Unity.Mathematics;
using static HexagonConfig;

public class HexagonData 
{
    public Location Location;
    public HexagonType Type;
    public float Height;
    public bool bIsMalaised;

    /** 
     * Converts the data into a transferable, lightweight object. 
     * Only contains data necessary for the minimap
     */
    public HexagonDTO GetDTO() {
        uint uType = (uint)Type;
        uint Malaise = (uint)(bIsMalaised ? 1 : 0) << 7;

        uint x = 130;
        x -= x & 0x10000000;
        return new HexagonDTO() {
            Type = uType + Malaise,
        };
    }
}
