using System;

public class BuildingConfig 
{
    [Flags]
    public enum Type : uint
    {
        // maximum supports 31 different buildings!
        DEFAULT = 0,
        Bakery = 1 << 0,
        Barracks = 1 << 1,
        Brewer = 1 << 2,
        Claypit = 1 << 3,
        Farm = 1 << 4,
        ForagersHut = 1 << 5,
        Forge = 1 << 6,
        Harbour = 1 << 7,
        HerbalistsHut = 1 << 8,
        HuntersHut = 1 << 9,
        MedicineHut = 1 << 10,
        Mill = 1 << 11,
        Mine = 1 << 12,
        Quarry = 1 << 13,
        Sawmill = 1 << 14,
        Scribe = 1 << 15,
        Smithy = 1 << 16,
        SmokeHut = 1 << 17,
        Stonemason = 1 << 18,
        Tailor = 1 << 19,
        Weaver = 1 << 20,
        Well = 1 << 21,
        Woodcutter = 1 << 22,
        Hut = 1 << 23,
        Merchant = 1 << 24,
        Library = 1 << 25,
    }

    public static int MaxIndex = 24;
    public static Type CategoryMeadowA = Type.Claypit | Type.Woodcutter | Type.ForagersHut | Type.Hut ;
    public static Type CategoryMeadowB = Type.HuntersHut | Type.Well;
    public static Type CategoryDesertA = Type.Quarry | Type.Sawmill | Type.Harbour;
    public static Type CategoryDesertB = Type.Farm | Type.HerbalistsHut | Type.MedicineHut | Type.Merchant;
    public static Type CategorySwampA = Type.Mine | Type.Forge | Type.Smithy | Type.Farm;
    public static Type CategorySwampB = Type.Barracks | Type.SmokeHut | Type.Mill | Type.Weaver | Type.Tailor;
    public static Type CategoryIceA = Type.Stonemason;
    public static Type CategoryIceB = Type.Bakery | Type.Brewer | Type.Scribe | Type.Library;

    public static Type UnlockOnStart = CategoryMeadowA;

}
