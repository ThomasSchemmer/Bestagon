using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

/**
 * A collection of Locations that are representing the same entity (for now building)
 * Provides access and comparing functions 
 * Main one is always at position 0!
 */
public class LocationSet : ISaveableData, IEnumerable<Location>
{
    public enum AreaSize
    {
        Single,
        Double,
        TripleLine,
        TripleCircle
    }

    private List<Location> Locations;

    public LocationSet(Location Location)
    {
        Locations = new()
        {
            Location
        };
    }

    // intentionally protected, only for internal handling/saving
    protected LocationSet()
    {
        Locations = new();
    }

    public void Add(Location Location)
    {
        Locations.Add(Location);
    }

    public LocationSet Copy()
    {
        LocationSet Copy = new();
        foreach (var Location in Locations)
        {
            Copy.Add(Location.Copy());
        }
        return Copy;
    }

    public bool Contains(Location Location)
    {
        return Locations.Any(Loc => Loc.Equals(Location));
    }

    public static bool TryGetAround(Location Location, AreaSize Type, out LocationSet Set)
    {
        return TryGetAround(Location, Type, Angle, out Set);
    }

    public static bool TryGetAround(Location Location, AreaSize Type, int Angle, out LocationSet Set)
    {
        Set = new(Location);
        if (Location == null)
            return false;

        switch (Type)
        {
            case AreaSize.Single: return true;
            case AreaSize.Double: return Set.TryMakeDouble(Location, Angle);
            case AreaSize.TripleLine: return Set.TryMakeTripleLine(Location, Angle);
            case AreaSize.TripleCircle: return Set.TryMakeTripleCircle(Location, Angle);
            default: return false;
        }
    }

    private bool TryMakeDouble(Location Location, int Angle)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        Location[] Directions = MapGenerator.GetDirections(Location);
        Location Neighbour = Location + Directions[Angle % Directions.Length];
        if (!HexagonConfig.IsValidLocation(Neighbour))
            return false;
        
        Add(Neighbour);
        return true;
    }

    private bool TryMakeTripleLine(Location Location, int Angle)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        Location[] DirectionsA = MapGenerator.GetDirections(Location);
        Location NeighbourA = Location + DirectionsA[Angle % DirectionsA.Length];
        if (!HexagonConfig.IsValidLocation(NeighbourA))
            return false;

        Location[] DirectionsB = MapGenerator.GetDirections(NeighbourA);
        Location NeighbourB = NeighbourA + DirectionsB[Angle % DirectionsB.Length];
        if (!HexagonConfig.IsValidLocation(NeighbourB))
            return false;

        Add(NeighbourA);
        Add(NeighbourB);
        return true;
    }

    private bool TryMakeTripleCircle(Location Location, int Angle)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        Location[] Directions = MapGenerator.GetDirections(Location);
        Location NeighbourA = Location + Directions[Angle % Directions.Length];
        Location NeighbourB = Location + Directions[(Angle + 1) % Directions.Length];
        if (!HexagonConfig.IsValidLocation(NeighbourA) || !HexagonConfig.IsValidLocation(NeighbourB))
            return false;

        Add(NeighbourA);
        Add(NeighbourB);
        return true;
    }

    public Location GetMainLocation()
    {
        return Locations[0];
    }

    public int Count()
    {
        return Locations.Count;
    }

    public int GetSize()
    {
        return GetStaticSize(Locations.Count);
    }

    public static int GetStaticSize(int Count)
    {
        //overall bytecount and location count
        int Size = sizeof(int) * 2;
        Size += Count * Location.GetStaticSize();
        return Size;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Locations.Count);

        foreach (var Location in Locations)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        }

        return Bytes.ToArray();
    }


    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int LocationCount);
        Locations = new(LocationCount);

        for (int i = 0; i < LocationCount; i++)
        {
            Location Location = Location.Zero;
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);
            Locations.Add(Location);
        }
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public HashSet<Location> ToHashSet()
    {
        return new HashSet<Location>(Locations);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Locations.GetEnumerator();
    }

    public IEnumerator<Location> GetEnumerator()
    {
        return Locations.GetEnumerator();
    }

    public static void SetAngle(int InAngle)
    {
        Angle = InAngle % MaxAngle;
    }

    public static void ResetAngle()
    {
        Angle = 0;
    }

    public static void IncreaseAngle()
    {
        SetAngle(Angle + 1);
    }

    public static int GetAngle()
    {
        return Angle;
    }

    public static int MaxCount = 3;
    private static int Angle = 0;
    private static int MaxAngle = 6;
}
