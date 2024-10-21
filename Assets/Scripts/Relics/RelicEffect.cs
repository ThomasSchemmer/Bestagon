using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Scriptable for Relics, used solely for defining. 
 * Must be created as ScriptableObject
 * Inherits from GameplayEffect to make use of a good buff/debuff applying system
 * Cannot be stored in savegame itself, only implicitly saved as @Unlockable type and loaded on runtime
 */ 
[CreateAssetMenu(fileName = "Relic", menuName = "ScriptableObjects/Relic", order = 10)]
public class RelicEffect : GameplayEffect
{
    public RelicType Type;
    public Sprite Image;
    public string Tooltip;

    public void BroadcastDiscoveryChanged(Unlockables.State State)
    {
        OnDiscoveryChanged.ForEach(_ => _?.Invoke(State));
    }

    public ActionList<Unlockables.State> OnDiscoveryChanged = new();

    public static RelicType CategoryMeadow = RelicType.WoodenMallet | RelicType.Calligulae | RelicType.Cradle | RelicType.Abacus;
    public static RelicType CategoryDesert = RelicType.Trowel | RelicType.Purse | RelicType.PrayerBeads;
    public static int CategoryCount = 2;
    public static int MaxIndex = 6;
}

[Flags]
public enum RelicType : uint
{
    // dont forget to list it in a category and update max index!
    DEFAULT = 255,
    Calligulae = 1 << 0,
    WoodenMallet = 1 << 1,
    Cradle = 1 << 2,
    Abacus = 1 << 3,
    Trowel = 1 << 4, 
    Purse = 1 << 5,
    PrayerBeads = 1 << 6,
}

