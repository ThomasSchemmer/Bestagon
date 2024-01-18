using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
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


    public bool TryGetHexagonHeightForHeight(float Height, out HexagonHeight HexHeight)
    {
        foreach (var Tuple in HeightMap.Tuples)
        {
            if (Tuple.Key.Contains(Height))
            {
                HexHeight = Tuple.Value;
                return true;
            }
        }

        HexHeight = default;
        return false;
    }

    public bool TryGetHexagonTypeForHeightOverride(HexagonHeight Height, out HexagonType HexType)
    {
        foreach (var Tuple in HeightOverrideMap.Tuples)
        {
            if (Tuple.Key == Height)
            {
                HexType = Tuple.Value;
                return true;
            }
        }

        HexType = default;
        return false;
    }

    public bool TryGetHexagonTypeForClimate(Climate Climate, out HexagonType HexType)
    {
        foreach (Biome Biome in ClimateMap)
        {
            if (Biome.Rect.Contains(Climate.Point))
            {
                HexType = Biome.HexagonType;
                return true;
            }
        }
        HexType = default;
        return false;
    }

    public bool TryGetClimateForHexagonType(HexagonType Type, out Climate Climate)
    {
        foreach (Biome Biome in ClimateMap)
        {
            if (Biome.HexagonType.HasFlag(Type))
            {
                Climate = new Climate(Biome.Rect.center);
                return true;
            }
        }

        Climate = new Climate(-1, -1);
        return false;
    }

}
