using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttributeSet", menuName = "ScriptableObjects/AttributeSet", order = 2)]
public class AttributeSet : ScriptableObject
{
    public SerializedDictionary<AttributeType, Attribute> Attributes = new();

    public void Initialize()
    {
        foreach (var Tuple in Attributes)
        {
            Tuple.Value.Initialize();
        }
    }

    public void Tick()
    {
        foreach (var Tuple in Attributes)
        {
            Attribute Attribute = Tuple.Value;
            float OldValue = Attribute.CurrentValue;
            Attribute.Tick();
            if (!Mathf.Approximately(OldValue, Attribute.CurrentValue))
            {
                _OnAnyAttributeChanged?.Invoke(Attribute);
            }
        }
    }

    // allows for lazy init
    public Attribute this[AttributeType Type]
    {
        get { 
            if (!Attributes.ContainsKey(Type))
            {
                Attributes.Add(Type, new Attribute(Type));
            }
            return Attributes[Type]; 
        }
        set
        {
            if (Attributes.ContainsKey(Type))
            {
                Attributes[Type] = value;
            }
            else
            {
                Attributes.Add(Type, value);
            }
        }
    }

    public void Reset()
    {
        foreach (var Attribute in Attributes)
        {
            Attribute.Value.Reset();
            _OnAnyAttributeChanged?.Invoke(Attribute.Value);
        }
    }

    public static AttributeSet Get()
    {
        if (GlobalAttributes == null)
        {
            GlobalAttributes = Resources.Load("GAS/AttributeSet") as AttributeSet;
        }
        return GlobalAttributes;
    }



    public delegate void OnAnyAttributeChanged(Attribute Attribute);
    public OnAnyAttributeChanged _OnAnyAttributeChanged;

    public static AttributeSet GlobalAttributes = null;

}
