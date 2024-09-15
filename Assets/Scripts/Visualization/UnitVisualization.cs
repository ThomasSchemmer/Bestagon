using System.IO;
using UnityEngine;

public class UnitVisualization : EntityVisualization<TokenizedUnitEntity>
{
    public void UpdateLocation() {
        transform.position = Entity.GetLocation().WorldLocation + Entity.GetOffset();
    }

}
