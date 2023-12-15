using System.Diagnostics;
using Unity.Mathematics;
using static HexagonConfig;

/** Includes all data necessary to display and update a hexagon */
public class HexagonData 
{
    public Location Location;
    public HexagonType Type;
    public HexagonHeight Height;
    public float Value;
    public float WorldHeight
    {
        get { return GetWorldHeightFromTile(new(Height, Type)); }
    }
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
