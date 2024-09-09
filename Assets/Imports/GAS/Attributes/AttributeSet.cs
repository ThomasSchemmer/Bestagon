using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "AttributeSet", menuName = "ScriptableObjects/AttributeSet", order = 2)]
public class AttributeSet : ScriptableObject
{
    public List<Attribute> Attributes = new();

    public void Initialize()
    {
        foreach (Attribute Attribute in Attributes)
        {
            Attribute.Initialize();
        }
    }

    public void Tick()
    {
        foreach (Attribute Attribute in Attributes)
        {
            float OldValue = Attribute.CurrentValue;
            Attribute.Tick();
            if (!Mathf.Approximately(OldValue, Attribute.CurrentValue))
            {
                _OnAnyAttributeChanged?.Invoke(Attribute);
            }
        }
    }

    public bool TryFind(string AttributeName, out Attribute FoundAttribute)
    {
        FoundAttribute = default;
        foreach (Attribute TargetAttribute in Attributes)
        {
            if (!TargetAttribute.Name.Equals(AttributeName))
                continue;

            FoundAttribute = TargetAttribute;
            return true;
        }

        return false;
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
