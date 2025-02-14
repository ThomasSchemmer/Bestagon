using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Provides access to ambers, which the player can find in the malaise
 * Once the Library has been built and the research done, Ambers can spawn inside the malaised area
 * On collecting the player can activate them to make the game harder
 * Once all are active (so a really hard game) and the player still manages to build a library, the player
 * can use the amber's power to develop a cure for the malaise and win the game
 */
public class AmberService : SaveableService
{
    [SaveableBaseType]
    public int AvailableAmberCount = -1;
    [SaveableBaseType]
    public int ActiveAmberCount = 0;
    [SaveableBaseType]
    public int ResearchTurns = 0;

    public SerializedDictionary<AttributeType, GameplayEffect> AvailableAmbers = new();
    [SaveableDictionary]
    public SerializedDictionary<AttributeType, AmberInfo> Infos = new();

    public GameObject ShowAmberButton;

    public bool IsUnlocked()
    {
        return AvailableAmberCount >= 0;
    }

    public bool CanHealMalaise() {
        return ActiveAmberCount == MaxAmount;
    }

    public void Unlock()
    {
        AvailableAmberCount = Mathf.Max(AvailableAmberCount, 0);
        ShowAmberButton.SetActive(true);
        _OnAmberAmountChanged?.Invoke(AvailableAmberCount);
    }

    public void Add(int i)
    {
        AvailableAmberCount += i;
        _OnAmberAmountChanged?.Invoke(AvailableAmberCount);
    }

    public bool ShouldSpawnMoreAmber()
    {
        if (!Game.TryGetService(out DecorationService Decorations))
            return false;

        int SpawnedCount = Decorations.GetAmountOfType((int)DecorationEntity.DType.Amber);
        int CurrentCount = AvailableAmberCount + SpawnedCount;
        if (CurrentCount >= MaxAmount)
            return false;

        return AvailableAmberCount + SpawnedCount < ActiveAmberCount + CollectableAmount;
    }

    public void ResearchTurn()
    {
        ResearchTurns++;
        if (ResearchTurns < MaxResearchTurnAmount)
            return;

        if (!IsUnlocked()) {
            Unlock();
        }
        else
        {
            Debug.Log("Congratz you fucking won");
        }

        ResearchTurns = 0;
    }

    public bool CanIncrease(AttributeType Type)
    {
        if (!IsUnlocked())
            return false;

        if (ActiveAmberCount + 1 > AvailableAmberCount)
            return false;

        if (!AvailableAmbers.ContainsKey(Type) || !Infos.ContainsKey(Type))
            return false;

        if (Infos[Type].CurrentValue + 1 > Infos[Type].MaxValue)
            return false;
        return true;
    }

    public void Increase(AttributeType Type)
    {
        if (!CanIncrease(Type))
            return;

        if (!TryGetPlayerGAB(out var GAS, out var PGA))
            return;

        GAS.TryApplyEffectTo(PGA, AvailableAmbers[Type]);
        Infos[Type].CurrentValue += 1;
        ActiveAmberCount++;
        _OnAmberAssigned?.Invoke();
    }

    public bool CanDecrease(AttributeType Type)
    {
        if (!IsUnlocked())
            return false;

        if (!AvailableAmbers.ContainsKey(Type) || !Infos.ContainsKey(Type))
            return false;

        if (Infos[Type].CurrentValue == 0)
            return false;

        return true;
    }

    public void Decrease(AttributeType Type)
    {
        if (!CanDecrease(Type))
            return;

        if (!TryGetPlayerGAB(out var _, out var PGA))
            return;

        PGA.RemoveEffect(AvailableAmbers[Type]);
        Infos[Type].CurrentValue -= 1;
        ActiveAmberCount--;
        _OnAmberAssigned?.Invoke();
    }

    private bool TryGetPlayerGAB(out GameplayAbilitySystem GAS, out GameplayAbilityBehaviour Behavior)
    {
        Behavior = default;
        GAS = default;
        if (!Game.TryGetService(out GAS))
            return false;

        GameObject Player = GameObject.Find("Player");
        if (Player == null)
            return false;

        Behavior = Player.GetComponent<GameplayAbilityBehaviour>();
        return Behavior != null;
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager SaveGameManager) =>
        {
            if (SaveGameManager.HasDataFor(SaveGameType.Amber))
                return;

            ShowAmberButton.SetActive(IsUnlocked());
            _OnInit?.Invoke(this);
        });
    }

    private void ApplyGameplayEffects()
    {
        if (IsInit)
            return;

        Game.RunAfterServiceInit((GameplayAbilitySystem GA) =>
        {
            if (!TryGetPlayerGAB(out var GAS, out var PGA))
                return;

            foreach (var Tuple in AvailableAmbers)
            {
                if (!Infos.ContainsKey(Tuple.Key))
                    continue;

                var Effect = Tuple.Value;
                var Info = Infos[Tuple.Key];
                for (int i = 0; i < Info.CurrentValue; i++)
                {
                    GAS.TryApplyEffectTo(PGA, Effect);
                }
            }
        });
    }

    protected override void ResetInternal()
    {
        AvailableAmbers.Clear();
    }

    protected override void StopServiceInternal(){}

    public override void OnAfterLoaded() {
        ApplyGameplayEffects();
        ShowAmberButton.SetActive(IsUnlocked());
        _OnInit?.Invoke(this);
    }

    [Serializable]
    public class AmberInfo
    {
        [SaveableBaseType]
        public int CurrentValue = 0;
        [SaveableBaseType]
        public int MaxValue = 0;
        [SaveableBaseType]
        public int BaseValue = 0;
    }

    public static int CollectableAmount = 2;
    public static int MaxAmount = 10;
    public static int MaxResearchTurnAmount = 3;

    public delegate void OnAmberAmountChanged(int Amount);
    public static OnAmberAmountChanged _OnAmberAmountChanged;

    public delegate void OnAmberAssigned();
    public static OnAmberAssigned _OnAmberAssigned;
}
