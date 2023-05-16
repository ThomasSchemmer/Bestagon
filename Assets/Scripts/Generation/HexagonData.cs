using Unity.Mathematics;
using static HexagonConfig;

public class HexagonData 
{
    public Location Location;
    public HexagonType Type;
    public bool bIsMalaised;

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
