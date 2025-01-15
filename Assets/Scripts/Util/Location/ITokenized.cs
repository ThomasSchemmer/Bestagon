using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Interface declaring anything with a location (except tiles) 
 * Can't be a class as entities are already a base class
 */
public interface ITokenized 
{
    public LocationSet GetLocations();
    public void SetLocation(LocationSet Location);
    public UnitEntity.UType GetUType();

}
