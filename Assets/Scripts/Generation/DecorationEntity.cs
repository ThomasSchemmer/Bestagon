using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static UnitEntity;

/** 
 * Entity representing decoration data on some hexagon, managed by @DecorationService
 * and visualized through chunk
 */
[CreateAssetMenu(fileName = "Decoration", menuName = "ScriptableObjects/Decoration", order = 4)]
[Serializable]
public class DecorationEntity : ScriptableEntity, ITokenized, IPreviewable
{
    public enum DType {
        DEFAULT = 0,
        Ruins = 1,
        Tribe = 2,
        Treasure = 3,
        Altar = 4
    }

    [SaveableEnum]
    public DType DecorationType;
    public DecorationVisualization Visualization;

    [SaveableClass]
    // decorations can only be in one spot
    private Location Location;
    [SaveableBaseType]
    private bool bIsActivated = false;
    [SaveableEnum]
    private CollectableChoice.ChoiceType ChoiceType;

    public DecorationEntity() {
        EntityType = EType.Decoration;
    }

    public void ApplyEffect(CollectableChoice.ChoiceType ChoiceType)
    {
        this.ChoiceType = ChoiceType;
        bIsActivated = true;
        if (!Game.TryGetService(out DecorationService Decorations))
            return;

        LocalizedGameplayEffect TargetEffect =
            ChoiceType == CollectableChoice.ChoiceType.Sacrifice ? Decorations.MalaiseImmunityEffect :
            ChoiceType == CollectableChoice.ChoiceType.Offering ? Decorations.ProductionIncreaseEffect :
            null; 
        
        if (TargetEffect == null)
            return;

        TargetEffect.ApplyToLocation(Location, 1);
    }

    private void RemoveEffect()
    {
        if (!Game.TryGetService(out DecorationService Decorations))
            return;

        LocalizedGameplayEffect TargetEffect =
            ChoiceType == CollectableChoice.ChoiceType.Sacrifice ? Decorations.MalaiseImmunityEffect :
            ChoiceType == CollectableChoice.ChoiceType.Offering ? Decorations.ProductionIncreaseEffect :
            null;

        if (TargetEffect == null)
            return;

        TargetEffect.RemoveFromLocation(Location);
    }

    public void OnDestroy()
    {
        RemoveEffect();
    }

    public bool ShouldBeKilledOnCollection()
    {
        return DecorationType != DType.Altar;
    }

    public void SetLocation(LocationSet Location)
    {
        this.Location = Location.GetMainLocation();
    }

    public LocationSet GetLocations()
    {
        return Location.ToSet();
    }

    public void SetVisualization(EntityVisualization Vis)
    {
        if (Vis is not DecorationVisualization)
            return;

        Visualization = Vis as DecorationVisualization;
    }

    public string GetDecorationText()
    {
        switch (DecorationType)
        {
            case DType.Ruins: return "Contains ancient ruins";
            case DType.Tribe: return "Contains unknown tribe";
            case DType.Treasure: return "Contains treasure chest";
            case DType.Altar: return GetAltarText();
            default: return "";
        }
    }

    public bool IsActivated()
    {
        return bIsActivated;
    }

    private string GetAltarText()
    {
        if (!bIsActivated)
            return "Contains mysterious altar";

        if (ChoiceType == CollectableChoice.ChoiceType.Offering)
            return "Altar increases productivity";

        return "Altar inhibits malaise spread";
    }

    public void OnLoaded()
    {
        if (bIsActivated)
        {
            ApplyEffect(ChoiceType);
        }
    }

    public virtual Vector3 GetOffset()
    {
        return new Vector3(0, 5, 0);
    }

    public virtual Quaternion GetRotation()
    {
        return Quaternion.Euler(0, 180, 0);
    }

    public override bool IsAboutToBeMalaised()
    {
        // while decorations can be destroyed, it is not the players unit
        return false;
    }

    public bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        throw new NotImplementedException();
    }

}
