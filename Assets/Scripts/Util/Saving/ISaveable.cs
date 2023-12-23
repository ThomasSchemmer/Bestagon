
using System;
using UnityEngine;

/** 
 * Provides access to save game information, such as id, size and data
 */
public interface ISaveable
{
    public abstract int GetSize();

    public abstract byte[] GetData();

    public abstract void SetData(byte[] Data);
}
