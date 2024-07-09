using System;
using System.Collections.Generic;
using UnityEngine;
using static HexagonConfig;

[CreateAssetMenu(fileName = "Biomes", menuName = "ScriptableObjects/Biomes", order = 0)]
public class BiomeMap : ScriptableObject
{
    public List<Biome> ClimateMap;

    public SerializedDictionary<HexagonHeight, HexagonType> HeightOverrideMap;
    public SerializedDictionary<FloatRange, HexagonHeight> HeightMap;

    public BiomeMap()
    {
        if (HeightOverrideMap == null)
        {
            HeightOverrideMap = new();
        }

        // hack to avoid unity serialized dic bug
        var Values = Enum.GetValues(typeof(HexagonHeight));
        foreach (var Value in Values)
        {
            HeightOverrideMap.Add((HexagonHeight)Value, HexagonType.Ocean);
        }
    }

    public void AddBiome(Biome Biome)
    {
        ClimateMap.Add(Biome);
    }
}
