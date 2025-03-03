using System;
using System.Collections.Generic;

public class Pathfinding
{
    public class Parameters
    {
        public bool bIsForLand;
        public bool bCheckReachable;
        public bool bTakeRawData;
        public bool bIgnoreMalaise;

        public static Parameters Standard = new();

        public Parameters(bool bIsForLand = true, bool bCheckReachable = true, bool bTakeRawData = false, bool bIgnoreMalaise = false)
        {
            this.bIsForLand = bIsForLand;
            this.bCheckReachable = bCheckReachable;
            this.bTakeRawData = bTakeRawData;
            this.bIgnoreMalaise = bIgnoreMalaise;
        }
    }

    public static List<Location> GetAffordableSubPath(List<Location> Path, int MovementPoints, Parameters Params)
    {
        List<Location> AffordablePath = new();
        if (Path.Count == 0)
            return AffordablePath;

        AffordablePath.Add(Path[0]);

        int PathCosts = 0;
        for(int i = 0; i < Path.Count - 1; i++)
        {
            List<Location> Move = new (){ Path[i], Path[i + 1]};
            int MoveCosts = GetCostsForPath(Move, Params);

            if (MoveCosts + PathCosts > MovementPoints)
                break;

            AffordablePath.Add(Path[i + 1]);
            PathCosts += MoveCosts;
        }

        return AffordablePath;
    }

    public static HashSet<Location> FindReachableLocationsFrom(Location Start, int Range, Parameters Params) {
        SearchFor(Start, null, Range, Params, out var LocationCosts, out var _);

        HashSet<Location> FoundLocations = new HashSet<Location>();
        foreach (Location FoundLocation in LocationCosts.Keys) {
            FoundLocations.Add(FoundLocation);
        }

        return FoundLocations;
    }

    public static List<Location> FindPathFromTo(Location Start, Location End, Parameters Params) {
        // potentially expensive flood filling!
        SearchFor(Start, End, -1, Params, out _, out var LocationComingFrom);

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

    public static int GetCostsForPath(List<Location> Path, Parameters Params) {
        if (Path.Count == 0)
            return -1;

        int Costs = 0;
        for (int i = 0; i < Path.Count - 1; i++) {
            Location Current = Path[i];
            Location Next = Path[i + 1];
            Costs += HexagonConfig.GetCostsFromTo(Current, Next, Params);
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
    private static void SearchFor(Location Start, Location End, int Range, Parameters Params, out Dictionary<Location, int> LocationCosts, out Dictionary<Location, Location> LocationComingFrom) {
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

                int NewCost = Params.bCheckReachable ? HexagonConfig.GetCostsFromTo(Current, Neighbour, Params) : 1;
                int NewTotalCost = LocationCosts[Current] + NewCost;
                if (Params.bCheckReachable && NewCost < 0)
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
