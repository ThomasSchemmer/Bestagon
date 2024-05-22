using System;
using System.Collections.Generic;
using Unity.Collections;

[Serializable]
/**
 * Wrapper to have defined access to production data
 */ 
public class Production : ISaveable
{
    [Serializable]
    public enum Type : uint
    {
        Wood = 0,
        Clay = 1,
        Stone = 2,
        Planks = 3,
        Tools = 4,
        Marble = 5,
        HeavyPlanks = 6,
        Mushroom = 7,
        Herbs = 8,
        Meat = 9,
        Jerky = 10,
        MeatPie = 11,
        WaterSkins = 12,
        Cloaks = 13,
        Coffee = 14,
        Medicine = 15,
        Scrolls = 16,
        Flour = 17,
        Fabric = 18,
        PlantFiber = 19,
        Hides = 20,
        IronBars = 21,
        Iron = 22,
        Grain = 23,
        HardWood = 24
    }

    /** Categories for the different production types. 
     * Value represents the minimum type index in the category 
     */
    public enum GoodsType : uint
    {
        BuildingMaterials = 0,
        Food = 7,
        LuxuryItems = 12, 
        TradeGoods = 17,
        MaxIndex = 25
    }

    public static int[] Indices = { 
        (int)GoodsType.BuildingMaterials, 
        (int)GoodsType.Food, 
        (int)GoodsType.LuxuryItems, 
        (int)GoodsType.TradeGoods,
        (int)GoodsType.MaxIndex
    };

    public Production()
    {
        _Production = new SerializedDictionary<Type, int>();
    }

    public Production(Type Type, int Amount) : this() {
        _Production.Add(Type, Amount);
    }

    public Production(Type[] Types, int[] Amounts) : this()
    {
        if (Types.Length != Amounts.Length)
            return;

        for (int i = 0; i < Types.Length; i++)
        {
            _Production.Add(Types[i], Amounts[i]);
        }
    }

    public Production(Tuple<Type, int>[] Tuples) : this()
    {
        foreach (Tuple<Type, int> Tuple in Tuples)
        {
            _Production.Add(Tuple.Key, Tuple.Value);
        }
    }

    public bool Contains(Type Type)
    {
        return _Production.ContainsKey(Type);
    }

    public bool IsEmpty()
    {
        return _Production.Count == 0;
    }

    public static Production operator +(Production A, Production B) {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production._Production.Add(Type, A[Type] + B[Type]);
        }
        return Production;
    }

    public static Production operator -(Production A, Production B) {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production._Production.Add(Type, A[Type] - B[Type]);
        }
        return Production;
    }

    public static Production operator *(int A, Production B)
    {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production._Production.Add(Type, A * B[Type]);
        }
        return Production;
    }

    public static bool operator <=(Production A, Production B) {
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            if (!(A[Type] <= B[Type]))
                return false;
        }
        return true;
    }

    public static bool operator >=(Production A, Production B)
    {
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            if (!(A[Type] >= B[Type]))
                return false;
        }
        return true;
    }

    public static Production operator* (Production A, int B)
    {
        return B * A;
    }

    public static bool operator <(Production A, Production B)
    {
        foreach (var Tuple in A.GetTuples())
        {
            if (!B.Contains(Tuple.Key))
                return false;

            if (A[Tuple.Key] >= B[Tuple.Key])
                return false;
        }
        return true;
    }

    public static bool operator >(Production A, Production B)
    {
        foreach (var Tuple in A.GetTuples())
        {
            if (!B.Contains(Tuple.Key))
                return false;

            if (A[Tuple.Key] <= B[Tuple.Key])
                return false;
        }
        return true;
    }

    public List<Tuple<Type, int>> GetTuples() {

        List<Tuple<Type, int>> Tuples = new();
        foreach (var Tuple in _Production)
        {
            if (Tuple.Value == 0)
                continue;

            Tuples.Add(new(Tuple.Key, Tuple.Value));
        }
        return Tuples;
    }

    public string GetDescription(Type Type) {
        return Type.ToString();
    }

    public string GetShortDescription() {
        string ProductionText = string.Empty;
        foreach (Type Type in Enum.GetValues(typeof(Type))) { 
            int Value = this[Type];
            if (Value == 0)
                continue;

            ProductionText += Value + GetShortDescription(Type) + " ";
        }

        return ProductionText;
    }

    public string GetShortDescription(Type Type) {
        return GetDescription(Type)[..1];
    }

    public override bool Equals(object obj)
    {
        if (obj is not Production)
            return false;

        Production Other = obj as Production;
        if (_Production.Tuples.Count != Other._Production.Tuples.Count)
            return false;

        foreach (var Tuple in _Production.Tuples)
        {
            if (!Other.Contains(Tuple.Key))
                return false;

            if (Other[Tuple.Key] != Tuple.Value)
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return _Production.GetHashCode();
    }

    public static int GetHungerFromFood(Type FoodType)
    {
        switch (FoodType)
        {
            case Type.Mushroom: return 1;
            case Type.Herbs: return 1;
            case Type.Meat: return 2;
            case Type.Jerky: return 3;
            case Type.MeatPie: return 5;
            default: return 0;
        }
    }

    public static Production Empty
    {
        get{
            return new Production();
        }
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // each type has a enum index and amount of this resource 
        return Enum.GetValues(typeof(Type)).Length * 2;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Type);
            Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)this[Type]);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes) {
        int Pos = 0;
        for (int i = 0; i < Enum.GetValues(typeof(Type)).Length; i++)
        {
            Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte iType);
            Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bValue);
            Type Type = (Type)iType;
            this[Type] = (int)bValue;
        }
    }

    public SerializedDictionary<Type, int> _Production;
    public int this[Type Type]
    {
        get { return _Production.ContainsKey(Type) ? _Production[Type] : 0; }
        set { 
            if (_Production.ContainsKey(Type))
            {
                _Production[Type] = value;
            }
            else
            {
                _Production.Add(Type, value);
            } 
        }
    }

}
