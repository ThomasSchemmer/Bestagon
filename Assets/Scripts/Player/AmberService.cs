using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmberService : SaveableService
{
    [SaveableBaseType]
    public int AmberCount = -1;
    [SaveableBaseType]
    public int ActiveAmberCount = 0;


    public bool IsUnlocked()
    {
        return AmberCount >= 0;
    }

    public void Unlock()
    {
        AmberCount = Mathf.Max(AmberCount, 0);
    }

    public void Add(int i)
    {
        AmberCount += i;
        _OnAmberAmountChanged?.Invoke(AmberCount);
    }

    public bool ShouldSpawnMoreAmber()
    {
        if (!Game.TryGetService(out DecorationService Decorations))
            return false;

        int SpawnedCount = Decorations.GetAmountOfType((int)DecorationEntity.DType.Amber);
        return AmberCount + SpawnedCount < ActiveAmberCount + CollectableAmount;
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager SaveGameManager) =>
        {
            if (SaveGameManager.HasDataFor(SaveGameType.Amber))
                return;

            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal()
    {
    }

    public override void OnAfterLoaded() { 
        _OnInit?.Invoke(this);
    }

    public static int CollectableAmount = 2;
    public delegate void OnAmberAmountChanged(int Amount);
    public static OnAmberAmountChanged _OnAmberAmountChanged;
}
