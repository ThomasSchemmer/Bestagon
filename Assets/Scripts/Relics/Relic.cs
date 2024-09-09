using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Relic", menuName = "ScriptableObjects/Relic", order = 10)]
public class Relic : GameplayEffect
{
    public string Name;
    public Sprite Image;
}
