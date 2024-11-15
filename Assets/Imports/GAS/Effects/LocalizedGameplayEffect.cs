using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * GE that only applies to certain Locations
 */
[CreateAssetMenu(fileName = "Localized Effect", menuName = "ScriptableObjects/LocalizedEffect", order = 11)]
public class LocalizedGameplayEffect : GameplayEffect
{
    private List<Tuple<Location, int>> BaseLocations = new();
    private HashSet<Location> AffectedLocations = new();

    public void ApplyToLocation(Location Location, int Range)
    {
        BaseLocations.Add(new(Location, Range));
        RefreshAffectedLocations();
    }

    public void RemoveFromLocation(Location Location)
    {
        for (int i = BaseLocations.Count - 1; i >= 0; i--)
        {
            if (!BaseLocations[i].Key.Equals(Location))
                continue;

            BaseLocations.RemoveAt(i);
        }
        RefreshAffectedLocations();
    }

    private void RefreshAffectedLocations()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        foreach (var AffectedLocation in AffectedLocations)
        {
            ApplyToLocation(MapGenerator, Location.Invalid, AffectedLocation);
        }

        AffectedLocations = new();
        foreach (var Tuple in BaseLocations) {
            var NewLocations = MapGenerator.GetNeighbourTileLocationsInRange(Tuple.Key.ToSet(), true, Tuple.Value);

            foreach (var Location in NewLocations)
            {
                ApplyToLocation(MapGenerator, Tuple.Key, Location);
            }

            AffectedLocations.UnionWith(NewLocations);
        }

        foreach (var Modifier in Modifiers)
        {
            Attribute Attribute = AttributeSet.Get()[Modifier.AttributeType];
            Attribute.ResetLocations();
            Attribute.ApplyTo(AffectedLocations);
        }
    }

    private void ApplyToLocation(MapGenerator MapGenerator, Location Source, Location Location)
    {
        // todo: this doesnt really account for multiple effects. If necessary, make the source 
        // a ptr to this effect etc
        // also make this a lookup for each source to neighbouring locations, so that only the ptr on the 
        // hex data is enough to get all info
        if (!MapGenerator.TryGetHexagonData(Location, out var Data))
            return;

        Data.SetAoESourceLocation(Source);
    }
}
