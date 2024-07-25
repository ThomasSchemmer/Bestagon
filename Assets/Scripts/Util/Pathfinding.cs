using System;
using System.Collections.Generic;

public class Pathfinding
{
    public static List<Location> GetAffordableSubPath(List<Location> Path, int MovementPoints)
    {
        List<Location> AffordablePath = new();
        if (Path.Count == 0)
            return AffordablePath;

        AffordablePath.Add(Path[0]);

        int PathCosts = 0;
        for(int i = 0; i < Path.Count - 1; i++)
        {
            List<Location> Move = new (){ Path[i], Path[i + 1]};
            int MoveCosts = GetCostsForPath(Move);

            if (MoveCosts + PathCosts > MovementPoints)
                break;

            AffordablePath.Add(Path[i + 1]);
            PathCosts += MoveCosts;
        }

        return AffordablePath;
    }

    public static HashSet<Location> FindReachableLocationsFrom(Location Start, int Range, bool bCheckReachable = true, bool bTakeRawData = false) {

        Dictionary<Location, int> LocationCosts;

        SearchFor(Start, null, Range, out LocationCosts, out var _, bCheckReachable, bTakeRawData);

        HashSet<Location> FoundLocations = new HashSet<Location>();
        foreach (Location FoundLocation in LocationCosts.Keys) {
            FoundLocations.Add(FoundLocation);
        }

        return FoundLocations;
    }

    public static List<Location> FindPathFromTo(Location Start, Location End, bool bCheckReachable = true) {
        // potentially expensive flood filling!
        Dictionary<Location, int> LocationCosts;
        Dictionary<Location, Location> LocationComingFrom;

        SearchFor(Start, End, -1, out LocationCosts, out LocationComingFrom, bCheckReachable);

        if (!LocationComingFrom.ContainsKey(End))
            return new List<Location>();

        List<Location> Path = new() {
            End
        };

        Location Previous = LocationComingFrom[End];
        while (Previous != null) {
            Path.Insert(0, Previous);
            Previous = LocationComingFrom[Previous];
        }

        return Path;
    }

    public static int GetCostsForPath(List<Location> Path) {
        if (Path.Count == 0)
            return -1;

        int Costs = 0;
        for (int i = 0; i < Path.Count - 1; i++) {
            Location Current = Path[i];
            Location Next = Path[i + 1];
            Costs += HexagonConfig.GetCostsFromTo(Current, Next);
        }

        return Costs;
    }

    /** 
     * Iterates over the map data and returns all found locations in range
     * Provides the total cost to reach each location, as well as its implicit best path
     * @bCheckReachable: if set only counts locations that are standardly reachable (ie no mountains/water..)
     * @bTakeRawData: if set only looks at the raw hexagon data for reachability (ie no malaise or transformed tiles)
     * Useful, as it does not need the MapGenerator to be initialized
     */
    private static void SearchFor(Location Start, Location End, int Range, out Dictionary<Location, int> LocationCosts, out Dictionary<Location, Location> LocationComingFrom, bool bCheckReachable = true, bool bTakeRawData = false) {
        LocationCosts = new();
        LocationComingFrom = new();
        PriorityQueue<Location> LocationsToCheck = new();

        LocationCosts.Add(Start, 0);
        LocationComingFrom.Add(Start, null);
        LocationsToCheck.Enqueue(Start, 0);

        int Iteration = 0;
        while (LocationsToCheck.Count > 0 && Iteration < MaxSearchIterations) {
            Iteration++;
            Tuple<Location, int> CurrentTuple = LocationsToCheck.Dequeue();
            Location Current = CurrentTuple.Key;

            List<Location> Neighbours = MapGenerator.GetNeighbourTileLocations(Current);
            foreach (Location Neighbour in Neighbours) {
                if (!HexagonConfig.IsValidLocation(Neighbour))
                    continue;

                int NewCost = bCheckReachable ? HexagonConfig.GetCostsFromTo(Current, Neighbour, bTakeRawData) : 1;
                int NewTotalCost = LocationCosts[Current] + NewCost;
                if (bCheckReachable && NewCost < 0)
                    continue;

                // out of reach
                if (Range >= 0 && NewTotalCost > Range)
                    continue;

                bool bWasVisited = LocationCosts.ContainsKey(Neighbour);
                bool bIsNewCheaper = bWasVisited ? LocationCosts[Neighbour] > NewTotalCost : true;

                if (bWasVisited && !bIsNewCheaper)
                    continue;

                LocationsToCheck.Enqueue(Neighbour, NewTotalCost);
                if (bWasVisited) {
                    LocationComingFrom.Remove(Neighbour);
                    LocationCosts.Remove(Neighbour);
                }
                LocationComingFrom.Add(Neighbour, Current);
                LocationCosts.Add(Neighbour, NewTotalCost);
                
                // early exit because we found our goal
                if (End != null && Neighbour.Equals(End))
                    return;
            }
        }
    }

    public static int MaxSearchIterations = 250;
}
