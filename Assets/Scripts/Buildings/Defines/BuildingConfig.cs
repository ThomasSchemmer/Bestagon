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
    }


    public static int CategoryAmount = 4;
    public static int MaxIndex = 22;
    public static Type CategoryMeadow = Type.Claypit | Type.Woodcutter | Type.ForagersHut | Type.HuntersHut | Type.Hut | Type.Well;
    public static Type CategoryDesert = Type.Quarry | Type.Sawmill | Type.Barracks | Type.HuntersHut | Type.Farm | Type.HerbalistsHut | Type.MedicineHut;
    public static Type CategorySwamp = Type.Mine | Type.Forge | Type.Smithy | Type.Harbour | Type.SmokeHut | Type.Mill | Type.Farm | Type.Weaver | Type.Tailor;
    public static Type CategoryIce = Type.Stonemason | Type.Bakery | Type.Brewer | Type.Scribe;
    public static Type[] Categories = new Type[4] { CategoryMeadow, CategoryDesert, CategorySwamp, CategoryIce};

    public static Type UnlockOnStart = Type.Claypit | Type.Woodcutter | Type.ForagersHut | Type.Hut;
}
