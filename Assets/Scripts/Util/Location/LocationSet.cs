using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/**
 * A collection of Locations that are representing the same entity (for now building)
 * Provides access and comparing functions 
 * Main one is always at position 0!
 */
public class LocationSet : IEnumerable<Location>
{
    public enum AreaSize : uint
    {
        Single = 0,
        Double = 1,
        TripleLine = 2,
        TripleCircle = 3
    }

    [SaveableList]
    private List<Location> Locations;

    public LocationSet(Location Location)
    {
        Locations = new()
        {
            Location
        };
    }

    // only for internal handling/saving
    public LocationSet()
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

    public bool ContainsAny(HashSet<Location> OtherLocations)
    {
        var Temp = Locations.ToHashSet();
        Temp.IntersectWith(OtherLocations);
        return Temp.Count > 0;
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

    public Location GetExtendedLocation()
    {
        if (Locations.Count < 2)
            return null;

        return Locations[1];
    }

    public int Count()
    {
        return Locations.Count;
    }

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
