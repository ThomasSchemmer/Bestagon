using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Interface declaring anything with a location (except tiles) 
 * Can't be a class as entities are already a base class
 */
public interface ITokenized 
{
    public Location GetLocation();
    public void SetLocation(Location Location);

    public void SetVisualization(EntityVisualization Vis);

}
